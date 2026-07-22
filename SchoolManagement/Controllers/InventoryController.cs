using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Controllers
{
    [ApiController, Authorize, Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _db;
        public InventoryController(AppDbContext db) => _db = db;
        private IActionResult Success(object? data, string message = "Success") => Ok(new { success = true, message, data });

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(int schoolId)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var products = _db.InventoryProducts.Where(x => x.SchoolId == schoolId && x.IsActive);
            return Success(new {
                totalProducts = await products.CountAsync(),
                totalBooks = await CountCategory(products, schoolId, "Book"),
                totalUniforms = await CountCategory(products, schoolId, "Uniform"),
                totalStudyMaterials = await CountCategory(products, schoolId, "Study"),
                lowStockItems = await products.CountAsync(x => x.CurrentStock - x.ReservedStock <= x.MinimumStock),
                todaySales = await _db.InventoryPayments.Where(x => x.PaymentDate.Date == today && _db.InventoryStudentOrders.Any(o => o.Id == x.StudentOrderId && o.SchoolId == schoolId)).SumAsync(x => x.Amount),
                pendingPayments = await _db.InventoryStudentOrders.Where(x => x.SchoolId == schoolId).SumAsync(x => x.TotalAmount - x.PaidAmount),
                monthlyRevenue = await _db.InventoryPayments.Where(x => x.PaymentDate >= monthStart && _db.InventoryStudentOrders.Any(o => o.Id == x.StudentOrderId && o.SchoolId == schoolId)).SumAsync(x => x.Amount)
            });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> Categories(int schoolId) => Success(await _db.InventoryCategories.Where(x => x.SchoolId == schoolId).OrderBy(x => x.Name).ToListAsync());
        [HttpPost("categories")]
        public async Task<IActionResult> Category(InventoryCategory item) { if (await _db.InventoryCategories.AnyAsync(x => x.SchoolId == item.SchoolId && x.Name == item.Name)) return Conflict(new { success=false, message="Category already exists." }); _db.Add(item); await _db.SaveChangesAsync(); return Success(item, "Category created."); }

        [HttpGet("vendors")]
        public async Task<IActionResult> Vendors(int schoolId) => Success(await _db.InventoryVendors.Where(x => x.SchoolId == schoolId).OrderBy(x => x.VendorName).ToListAsync());
        [HttpPost("vendors")]
        public async Task<IActionResult> Vendor(InventoryVendor item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item, "Vendor created."); }

        [HttpGet("products")]
        public async Task<IActionResult> Products(int schoolId) => Success(await (from p in _db.InventoryProducts join c in _db.InventoryCategories on p.CategoryId equals c.Id where p.SchoolId == schoolId orderby p.ProductName select new { p.Id,p.SchoolId,p.ProductCode,p.ProductName,p.CategoryId,categoryName=c.Name,p.Brand,p.Unit,p.HsnCode,p.PurchasePrice,p.SellingPrice,p.GstPercent,p.CurrentStock,p.ReservedStock,availableStock=p.CurrentStock-p.ReservedStock,p.IssuedStock,p.MinimumStock,p.Barcode,p.IsActive }).ToListAsync());
        [HttpPost("products")]
        public async Task<IActionResult> Product(InventoryProduct item) { if (await _db.InventoryProducts.AnyAsync(x => x.SchoolId == item.SchoolId && x.ProductCode == item.ProductCode)) return Conflict(new { success=false,message="Product code already exists." }); _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Product created."); }

        [HttpGet("variants")]
        public async Task<IActionResult> Variants(int schoolId) => Success(await (from v in _db.InventoryProductVariants join p in _db.InventoryProducts on v.ProductId equals p.Id where p.SchoolId == schoolId select new {v.Id,v.ProductId,productName=p.ProductName,v.VariantName,v.Sku,v.Barcode,v.CurrentStock,v.ReservedStock,availableStock=v.CurrentStock-v.ReservedStock,v.PriceAdjustment,v.IsActive}).ToListAsync());
        [HttpPost("variants")]
        public async Task<IActionResult> Variant(InventoryProductVariant item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Variant created."); }

        [HttpGet("books")]
        public async Task<IActionResult> Books(int schoolId) => Success(await (from b in _db.InventoryBooks join p in _db.InventoryProducts on b.ProductId equals p.Id where b.SchoolId == schoolId select new {b.Id,b.ProductId,bookName=p.ProductName,b.AcademicSessionId,b.ClassId,b.SectionId,b.SubjectId,b.Publisher,b.Edition,b.Isbn,p.SellingPrice}).ToListAsync());
        [HttpPost("books")]
        public async Task<IActionResult> Book(InventoryBook item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Book mapping created."); }

        [HttpGet("kits")]
        public async Task<IActionResult> Kits(int schoolId) => Success(await _db.InventoryKits.Where(x => x.SchoolId == schoolId).OrderBy(x => x.KitName).ToListAsync());
        [HttpPost("kits")]
        public async Task<IActionResult> Kit(InventoryKit item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Kit created."); }

        [HttpGet("purchase-orders")]
        public async Task<IActionResult> PurchaseOrders(int schoolId) => Success(await (from po in _db.InventoryPurchaseOrders join v in _db.InventoryVendors on po.VendorId equals v.Id where po.SchoolId == schoolId orderby po.PurchaseDate descending select new {po.Id,po.PoNumber,vendorName=v.VendorName,po.PurchaseDate,po.InvoiceNumber,po.Status,po.TotalAmount}).ToListAsync());
        [HttpPost("purchase-orders")]
        public async Task<IActionResult> PurchaseOrder(PurchaseOrderRequest request)
        {
            if (!request.Items.Any()) return BadRequest(new {success=false,message="At least one product is required."});
            await using var tx=await _db.Database.BeginTransactionAsync();
            var po=new InventoryPurchaseOrder{SchoolId=request.SchoolId,VendorId=request.VendorId,PoNumber=request.PoNumber??$"PO{DateTime.Now:yyyyMMddHHmmss}",PurchaseDate=request.PurchaseDate,InvoiceNumber=request.InvoiceNumber,Status="Received",TotalAmount=request.Items.Sum(x=>x.Quantity*x.Rate*(1+x.GstPercent/100)-x.Discount)};
            _db.Add(po); await _db.SaveChangesAsync();
            foreach(var x in request.Items){_db.Add(new InventoryPurchaseOrderItem{PurchaseOrderId=po.Id,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,Quantity=x.Quantity,Rate=x.Rate,GstPercent=x.GstPercent,Discount=x.Discount}); await AddStock(request.SchoolId,x.ProductId,x.ProductVariantId,x.Quantity,"Purchase",po.Id);}
            await _db.SaveChangesAsync(); await tx.CommitAsync(); return Success(po,"Purchase received and stock updated.");
        }

        [HttpGet("stock")]
        public async Task<IActionResult> Stock(int schoolId) => await Products(schoolId);

        [HttpGet("orders")]
        public async Task<IActionResult> Orders(int schoolId) => Success(await (from o in _db.InventoryStudentOrders join s in _db.Students on o.StudentId equals s.Id where o.SchoolId==schoolId orderby o.OrderDate descending select new{o.Id,o.OrderNumber,studentName=s.StudentName,o.OrderDate,o.Status,o.TotalAmount,o.PaidAmount,balance=o.TotalAmount-o.PaidAmount}).ToListAsync());
        [HttpPost("orders")]
        public async Task<IActionResult> Order(StudentOrderRequest request)
        {
            if(!request.Items.Any()) return BadRequest(new{success=false,message="Order has no items."});
            await using var tx=await _db.Database.BeginTransactionAsync(); decimal total=0;
            foreach(var x in request.Items){var p=await _db.InventoryProducts.FindAsync(x.ProductId); if(p==null||p.CurrentStock-p.ReservedStock<x.Quantity)return Conflict(new{success=false,message=$"Insufficient stock for product {x.ProductId}."}); p.ReservedStock+=x.Quantity; total+=x.Quantity*(x.UnitPrice??p.SellingPrice)-x.Discount;}
            var enrollment=await _db.StudentEnrollment.FirstOrDefaultAsync(x=>(request.EnrollmentId>0?x.Id==request.EnrollmentId:x.StudentId==request.StudentId)&&x.StudentId==request.StudentId&&x.SchoolId==request.SchoolId&&x.SessionId==request.AcademicSessionId&&x.IsActive);
            if(enrollment==null)return BadRequest(new{success=false,message="A valid active enrollment is required for this order."});
            var order=new InventoryStudentOrder{SchoolId=request.SchoolId,AcademicSessionId=request.AcademicSessionId,StudentId=request.StudentId,EnrollmentId=enrollment.Id,OrderNumber=$"SO{DateTime.Now:yyyyMMddHHmmss}",Status="Approved",TotalAmount=total}; _db.Add(order);await _db.SaveChangesAsync();
            foreach(var x in request.Items){var p=await _db.InventoryProducts.FindAsync(x.ProductId);_db.Add(new InventoryStudentOrderItem{StudentOrderId=order.Id,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,Quantity=x.Quantity,UnitPrice=x.UnitPrice??p!.SellingPrice,GstPercent=p!.GstPercent,Discount=x.Discount});}
            await _db.SaveChangesAsync();await tx.CommitAsync();return Success(order,"Order approved and stock reserved.");
        }

        [HttpPost("orders/{orderId:int}/issue")]
        public async Task<IActionResult> Issue(int orderId)
        {
            var order=await _db.InventoryStudentOrders.FindAsync(orderId);if(order==null)return NotFound();if(order.Status=="Issued")return Conflict(new{success=false,message="Order already issued."});
            var items=await _db.InventoryStudentOrderItems.Where(x=>x.StudentOrderId==orderId).ToListAsync();
            foreach(var x in items){var p=await _db.InventoryProducts.FindAsync(x.ProductId);if(p==null||p.CurrentStock<x.Quantity)return Conflict(new{success=false,message="Insufficient stock."});p.CurrentStock-=x.Quantity;p.ReservedStock=Math.Max(0,p.ReservedStock-x.Quantity);p.IssuedStock+=x.Quantity;_db.Add(new InventoryStockTransaction{SchoolId=order.SchoolId,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,TransactionType="OUT",Quantity=x.Quantity,ReferenceType="StudentOrder",ReferenceId=order.Id});}
            order.Status="Issued";await _db.SaveChangesAsync();return Success(order,"Items issued.");
        }

        [HttpGet("payments")]
        public async Task<IActionResult> Payments(int schoolId)=>Success(await(from p in _db.InventoryPayments join o in _db.InventoryStudentOrders on p.StudentOrderId equals o.Id join s in _db.Students on o.StudentId equals s.Id where o.SchoolId==schoolId orderby p.PaymentDate descending select new{p.Id,p.ReceiptNumber,studentName=s.StudentName,p.Amount,p.PaymentDate,p.PaymentMode,p.ReferenceNumber}).ToListAsync());
        [HttpPost("payments")]
        public async Task<IActionResult> Payment(InventoryPayment item){var o=await _db.InventoryStudentOrders.FindAsync(item.StudentOrderId);if(o==null)return NotFound(new{success=false,message="Order not found."});item.ReceiptNumber=string.IsNullOrWhiteSpace(item.ReceiptNumber)?$"ST{DateTime.Now:yyyyMMddHHmmss}":item.ReceiptNumber;o.PaidAmount+=item.Amount;_db.Add(item);await _db.SaveChangesAsync();return Success(item,"Payment recorded.");}

        [HttpGet("returns")]
        public async Task<IActionResult> Returns(int schoolId)=>Success(await _db.InventoryReturns.Where(x=>x.SchoolId==schoolId).OrderByDescending(x=>x.ReturnDate).ToListAsync());
        [HttpPost("returns")]
        public async Task<IActionResult> Return(InventoryReturnRequest request){var ret=new InventoryReturn{SchoolId=request.SchoolId,StudentOrderId=request.StudentOrderId,ReturnNumber=$"RT{DateTime.Now:yyyyMMddHHmmss}",Reason=request.Reason};_db.Add(ret);await _db.SaveChangesAsync();foreach(var x in request.Items){_db.Add(new InventoryReturnItem{ReturnId=ret.Id,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,Quantity=x.Quantity,Condition=x.Condition,Restock=x.Restock});if(x.Restock)await AddStock(request.SchoolId,x.ProductId,x.ProductVariantId,x.Quantity,"Return",ret.Id);}await _db.SaveChangesAsync();return Success(ret,"Return recorded and eligible stock restored.");}

        [HttpGet("reports")]
        public async Task<IActionResult> Reports(int schoolId)=>Success(new{inventoryValue=await _db.InventoryProducts.Where(x=>x.SchoolId==schoolId).SumAsync(x=>x.CurrentStock*x.PurchasePrice),salesValue=await _db.InventoryStudentOrders.Where(x=>x.SchoolId==schoolId).SumAsync(x=>x.TotalAmount),collected=await _db.InventoryStudentOrders.Where(x=>x.SchoolId==schoolId).SumAsync(x=>x.PaidAmount),lowStock=await _db.InventoryProducts.CountAsync(x=>x.SchoolId==schoolId&&x.CurrentStock-x.ReservedStock<=x.MinimumStock)});

        private async Task<int> CountCategory(IQueryable<InventoryProduct> products,int schoolId,string term){var ids=await _db.InventoryCategories.Where(x=>x.SchoolId==schoolId&&x.Name.Contains(term)).Select(x=>x.Id).ToListAsync();return await products.CountAsync(x=>ids.Contains(x.CategoryId));}
        private async Task AddStock(int schoolId,int productId,int? variantId,decimal qty,string reference,int referenceId){var p=await _db.InventoryProducts.FindAsync(productId)??throw new InvalidOperationException("Product not found.");p.CurrentStock+=qty;if(variantId.HasValue){var v=await _db.InventoryProductVariants.FindAsync(variantId.Value)??throw new InvalidOperationException("Variant not found.");v.CurrentStock+=qty;}_db.Add(new InventoryStockTransaction{SchoolId=schoolId,ProductId=productId,ProductVariantId=variantId,TransactionType="IN",Quantity=qty,ReferenceType=reference,ReferenceId=referenceId});}
    }
}
