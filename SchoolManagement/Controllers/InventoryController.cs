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

        [HttpGet("study-materials-dashboard")]
        public async Task<IActionResult> StudyMaterialsDashboard(int schoolId) => Success(new {
            totalBooks = await _db.InventoryBooks.CountAsync(x => x.SchoolId == schoolId),
            totalBookKits = await _db.InventoryKits.CountAsync(x => x.SchoolId == schoolId && x.KitType == "Book" && x.IsActive),
            totalUniformKits = await _db.InventoryKits.CountAsync(x => x.SchoolId == schoolId && x.KitType == "Uniform" && x.IsActive),
            classesCovered = await _db.InventoryBooks.Where(x => x.SchoolId == schoolId && x.ClassId != null).Select(x => x.ClassId).Distinct().CountAsync(),
            averageBookPrice = await _db.InventoryBooks.Where(x => x.SchoolId == schoolId).Select(x => (decimal?)(x.SellingPrice - x.DiscountAmount)).AverageAsync() ?? 0,
            totalBookValue = await _db.InventoryBooks.Where(x => x.SchoolId == schoolId).SumAsync(x => x.SellingPrice - x.DiscountAmount),
            totalKitValue = await _db.InventoryKits.Where(x => x.SchoolId == schoolId && x.IsActive).SumAsync(x => x.SellingPrice - x.DiscountAmount)
        });

        [HttpGet("categories")]
        public async Task<IActionResult> Categories(int schoolId) => Success(await _db.InventoryCategories.Where(x => x.SchoolId == schoolId).OrderBy(x => x.Name).ToListAsync());
        [HttpPost("categories")]
        public async Task<IActionResult> Category(InventoryCategory item) { if (await _db.InventoryCategories.AnyAsync(x => x.SchoolId == item.SchoolId && x.Name == item.Name)) return Conflict(new { success=false, message="Category already exists." }); _db.Add(item); await _db.SaveChangesAsync(); return Success(item, "Category created."); }
        [HttpPut("categories/{id:int}")]
        public Task<IActionResult> UpdateCategory(int id, InventoryCategory item) => UpdateMaster(_db.InventoryCategories,id,item,"Category");

        [HttpGet("vendors")]
        public async Task<IActionResult> Vendors(int schoolId) => Success(await _db.InventoryVendors.Where(x => x.SchoolId == schoolId).OrderBy(x => x.VendorName).ToListAsync());
        [HttpPost("vendors")]
        public async Task<IActionResult> Vendor(InventoryVendor item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item, "Vendor created."); }
        [HttpPut("vendors/{id:int}")]
        public Task<IActionResult> UpdateVendor(int id, InventoryVendor item) => UpdateMaster(_db.InventoryVendors,id,item,"Vendor");

        [HttpGet("products")]
        public async Task<IActionResult> Products(int schoolId) => Success(await (from p in _db.InventoryProducts join c in _db.InventoryCategories on p.CategoryId equals c.Id where p.SchoolId == schoolId orderby p.ProductName select new { p.Id,p.SchoolId,p.ProductCode,p.ProductName,p.CategoryId,categoryName=c.Name,p.Brand,quantity=p.Unit,p.HsnCode,p.PurchasePrice,p.SellingPrice,p.GstPercent,p.CurrentStock,p.ReservedStock,purchasedQuantity=_db.InventoryStockTransactions.Where(t=>t.ProductId==p.Id&&t.TransactionType=="IN"&&t.ReferenceType=="Purchase").Sum(t=>(decimal?)t.Quantity)??0,remainingQuantity=p.CurrentStock-p.ReservedStock,stockStatus=p.CurrentStock-p.ReservedStock<1?"Out of Stock":"In Stock",p.IssuedStock,p.MinimumStock,p.Barcode,p.IsActive }).ToListAsync());
        [HttpPost("products")]
        public async Task<IActionResult> Product(InventoryProduct item) { if (await _db.InventoryProducts.AnyAsync(x => x.SchoolId == item.SchoolId && x.ProductCode == item.ProductCode)) return Conflict(new { success=false,message="Product code already exists." }); _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Product created."); }
        [HttpPut("products/{id:int}")]
        public Task<IActionResult> UpdateProduct(int id, InventoryProduct item) => UpdateMaster(_db.InventoryProducts,id,item,"Product");

        [HttpGet("variants")]
        public async Task<IActionResult> Variants(int schoolId) => Success(await (from v in _db.InventoryProductVariants join p in _db.InventoryProducts on v.ProductId equals p.Id where p.SchoolId == schoolId select new {v.Id,v.ProductId,productName=p.ProductName,v.VariantName,v.Sku,v.Barcode,v.CurrentStock,v.ReservedStock,availableStock=v.CurrentStock-v.ReservedStock,v.PriceAdjustment,v.IsActive}).ToListAsync());
        [HttpPost("variants")]
        public async Task<IActionResult> Variant(InventoryProductVariant item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Variant created."); }
        [HttpPut("variants/{id:int}")]
        public Task<IActionResult> UpdateVariant(int id, InventoryProductVariant item) => UpdateMaster(_db.InventoryProductVariants,id,item,"Variant");

        [HttpGet("books")]
        public async Task<IActionResult> Books(int schoolId) => Success(await (from b in _db.InventoryBooks join s in _db.AcademicSessions on b.AcademicSessionId equals s.Id where b.SchoolId == schoolId orderby b.BookName select new { b.Id, b.BookName, b.AcademicSessionId, AcademicSession = s.Year_Start.Year + "-" + s.Year_End.Year, b.ClassId, b.SectionId, b.SubjectId, b.Publisher, b.Edition, b.Isbn, b.Mrp, b.SellingPrice, b.DiscountAmount, FinalPrice = b.SellingPrice - b.DiscountAmount }).ToListAsync());
        [HttpPost("books")]
        public async Task<IActionResult> Book(InventoryBook item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Book mapping created."); }
        [HttpPut("books/{id:int}")]
        public Task<IActionResult> UpdateBook(int id, InventoryBook item) => UpdateMaster(_db.InventoryBooks,id,item,"Book mapping");

        [HttpGet("kits")]
        public async Task<IActionResult> Kits(int schoolId) => Success(await _db.InventoryKits.Where(x => x.SchoolId == schoolId).OrderBy(x => x.KitName).ToListAsync());
        [HttpPost("kits")]
        public async Task<IActionResult> Kit(InventoryKit item) { _db.Add(item); await _db.SaveChangesAsync(); return Success(item,"Kit created."); }
        [HttpPut("kits/{id:int}")]
        public Task<IActionResult> UpdateKit(int id, InventoryKit item) => UpdateMaster(_db.InventoryKits,id,item,"Kit");

        [HttpGet("book-kits")]
        public async Task<IActionResult> BookKits(int schoolId) => Success(await (from x in _db.InventoryKits join s in _db.AcademicSessions on x.AcademicSessionId equals s.Id where x.SchoolId == schoolId && x.KitType == "Book" orderby x.KitName select new { x.Id, x.KitName, x.AcademicSessionId, AcademicSession = s.Year_Start.Year + "-" + s.Year_End.Year, x.ClassId, x.Mrp, x.SellingPrice, x.DiscountAmount, FinalPrice = x.SellingPrice - x.DiscountAmount, x.IsActive }).ToListAsync());
        [HttpPost("book-kits")]
        public async Task<IActionResult> BookKit(InventoryKit item) { item.KitType = "Book"; return await SavePricedKit(item); }
        [HttpPut("book-kits/{id:int}")]
        public async Task<IActionResult> UpdateBookKit(int id, InventoryKit input) => await UpdatePricedKit(id,input,"Book");

        [HttpGet("uniform-kits")]
        public async Task<IActionResult> UniformKits(int schoolId) => Success(await (from x in _db.InventoryKits join s in _db.AcademicSessions on x.AcademicSessionId equals s.Id where x.SchoolId == schoolId && x.KitType == "Uniform" orderby x.KitName select new { x.Id, x.KitName, x.AcademicSessionId, AcademicSession = s.Year_Start.Year + "-" + s.Year_End.Year, x.ClassId, x.Mrp, x.SellingPrice, x.DiscountAmount, FinalPrice = x.SellingPrice - x.DiscountAmount, x.IsActive }).ToListAsync());
        [HttpPost("uniform-kits")]
        public async Task<IActionResult> UniformKit(InventoryKit item) { item.KitType = "Uniform"; return await SavePricedKit(item); }
        [HttpPut("uniform-kits/{id:int}")]
        public async Task<IActionResult> UpdateUniformKit(int id, InventoryKit input) => await UpdatePricedKit(id,input,"Uniform");

        [HttpGet("purchase-orders")]
        public async Task<IActionResult> PurchaseOrders(int schoolId) => Success(await (from po in _db.InventoryPurchaseOrders join v in _db.InventoryVendors on po.VendorId equals v.Id where po.SchoolId == schoolId orderby po.PurchaseDate descending select new {po.Id,po.PoNumber,vendorName=v.VendorName,po.PurchaseDate,po.InvoiceNumber,po.Status,po.TotalAmount}).ToListAsync());
        [HttpPost("purchase-orders")]
        public async Task<IActionResult> PurchaseOrder(PurchaseOrderRequest request)
        {
            if (!request.Items.Any()) return BadRequest(new {success=false,message="At least one product is required."});
            if (!await _db.InventoryVendors.AnyAsync(x => x.Id == request.VendorId && x.SchoolId == request.SchoolId && x.IsActive))
                return BadRequest(new {success=false,message="A valid active vendor is required."});
            if (request.Items.Any(x => x.Quantity <= 0 || x.Rate < 0 || x.GstPercent < 0 || x.Discount < 0))
                return BadRequest(new {success=false,message="Purchase quantities must be positive and amounts cannot be negative."});
            var purchaseProductIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
            if (await _db.InventoryProducts.CountAsync(x => purchaseProductIds.Contains(x.Id) && x.SchoolId == request.SchoolId) != purchaseProductIds.Count)
                return BadRequest(new {success=false,message="One or more products do not belong to this school."});
            await using var tx=await _db.Database.BeginTransactionAsync();
            var po=new InventoryPurchaseOrder{SchoolId=request.SchoolId,VendorId=request.VendorId,PoNumber=request.PoNumber??$"PO{DateTime.Now:yyyyMMddHHmmss}",PurchaseDate=request.PurchaseDate,InvoiceNumber=request.InvoiceNumber,Status="Received",TotalAmount=request.Items.Sum(x=>x.Quantity*x.Rate*(1+x.GstPercent/100)-x.Discount)};
            _db.Add(po); await _db.SaveChangesAsync();
            foreach(var x in request.Items){_db.Add(new InventoryPurchaseOrderItem{PurchaseOrderId=po.Id,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,Quantity=x.Quantity,Rate=x.Rate,GstPercent=x.GstPercent,Discount=x.Discount}); await AddStock(request.SchoolId,x.ProductId,x.ProductVariantId,x.Quantity,"Purchase",po.Id);}
            await _db.SaveChangesAsync(); await tx.CommitAsync(); return Success(po,"Purchase received and stock updated.");
        }

        [HttpGet("stock")]
        public async Task<IActionResult> Stock(int schoolId) => await Products(schoolId);

        [HttpPost("stock-adjustments")]
        public async Task<IActionResult> AdjustStock(InventoryStockAdjustmentRequest request)
        {
            if (request.Quantity <= 0 || (request.TransactionType != "IN" && request.TransactionType != "OUT"))
                return BadRequest(new { success=false, message="A positive quantity and transaction type IN or OUT are required." });
            var product = await _db.InventoryProducts.FirstOrDefaultAsync(x => x.Id == request.ProductId && x.SchoolId == request.SchoolId);
            if (product == null) return BadRequest(new { success=false, message="Product not found for this school." });
            var signedQuantity = request.TransactionType == "IN" ? request.Quantity : -request.Quantity;
            if (product.CurrentStock + signedQuantity < product.ReservedStock)
                return Conflict(new { success=false, message="Adjustment would reduce stock below the reserved quantity." });
            product.CurrentStock += signedQuantity;
            if (request.ProductVariantId.HasValue)
            {
                var variant = await _db.InventoryProductVariants.FirstOrDefaultAsync(x => x.Id == request.ProductVariantId && x.ProductId == request.ProductId);
                if (variant == null) return BadRequest(new { success=false, message="Variant does not belong to the selected product." });
                if (variant.CurrentStock + signedQuantity < variant.ReservedStock)
                    return Conflict(new { success=false, message="Adjustment would reduce variant stock below its reserved quantity." });
                variant.CurrentStock += signedQuantity;
            }
            _db.Add(new InventoryStockTransaction { SchoolId=request.SchoolId, ProductId=request.ProductId, ProductVariantId=request.ProductVariantId,
                TransactionType=request.TransactionType, Quantity=request.Quantity, ReferenceType="ManualAdjustment", Remarks=request.Remarks });
            await _db.SaveChangesAsync(); return Success(product, "Stock adjusted.");
        }

        [HttpGet("orders")]
        public async Task<IActionResult> Orders(int schoolId) => Success(await (from o in _db.InventoryStudentOrders join s in _db.Students on o.StudentId equals s.Id where o.SchoolId==schoolId orderby o.OrderDate descending select new{o.Id,o.OrderNumber,studentName=s.StudentName,o.OrderDate,o.Status,o.TotalAmount,o.PaidAmount,balance=o.TotalAmount-o.PaidAmount}).ToListAsync());
        [HttpGet("orders/{orderId:int}/items")]
        public async Task<IActionResult> OrderItems(int orderId) => Success(await (from i in _db.InventoryStudentOrderItems join p in _db.InventoryProducts on i.ProductId equals p.Id where i.StudentOrderId==orderId select new{i.Id,i.ProductId,i.ProductVariantId,p.ProductName,i.Quantity,i.UnitPrice,i.GstPercent,i.Discount}).ToListAsync());
        [HttpPost("orders")]
        public async Task<IActionResult> Order(StudentOrderRequest request)
        {
            if(!request.Items.Any()) return BadRequest(new{success=false,message="Order has no items."});
            if(request.OrderType != "For Sale" && request.OrderType != "For Use") return BadRequest(new{success=false,message="Order type must be For Sale or For Use."});
            if(request.OrderType == "For Use" && (!request.BorrowDateTime.HasValue || !request.ReturnDateTime.HasValue || request.ReturnDateTime <= request.BorrowDateTime)) return BadRequest(new{success=false,message="A return date after the borrow date is required for temporary use."});
            await using var tx=await _db.Database.BeginTransactionAsync(); decimal total=0;
            foreach(var x in request.Items){var p=await _db.InventoryProducts.FirstOrDefaultAsync(p=>p.Id==x.ProductId&&p.SchoolId==request.SchoolId); if(p==null||p.CurrentStock-p.ReservedStock<x.Quantity)return Conflict(new{success=false,message=$"Insufficient stock for product {x.ProductId}."}); p.CurrentStock-=x.Quantity;p.IssuedStock+=x.Quantity;if(request.OrderType=="For Sale")total+=x.Quantity*(x.UnitPrice??p.SellingPrice)-x.Discount;}
            var enrollment=await _db.StudentEnrollment.FirstOrDefaultAsync(x=>(request.EnrollmentId>0?x.Id==request.EnrollmentId:x.StudentId==request.StudentId)&&x.StudentId==request.StudentId&&x.SchoolId==request.SchoolId&&x.SessionId==request.AcademicSessionId&&x.IsActive);
            if(enrollment==null)return BadRequest(new{success=false,message="A valid active enrollment is required for this order."});
            var order=new InventoryStudentOrder{SchoolId=request.SchoolId,AcademicSessionId=request.AcademicSessionId,StudentId=request.StudentId,EnrollmentId=enrollment.Id,OrderNumber=$"SO{DateTime.Now:yyyyMMddHHmmss}",Status=request.OrderType=="For Use"?"Borrowed":"Issued",OrderType=request.OrderType,BorrowDateTime=request.BorrowDateTime,ReturnDateTime=request.ReturnDateTime,TotalAmount=total}; _db.Add(order);await _db.SaveChangesAsync();
            foreach(var x in request.Items){var p=await _db.InventoryProducts.FindAsync(x.ProductId);_db.Add(new InventoryStudentOrderItem{StudentOrderId=order.Id,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,Quantity=x.Quantity,UnitPrice=request.OrderType=="For Sale"?(x.UnitPrice??p!.SellingPrice):0,GstPercent=request.OrderType=="For Sale"?p!.GstPercent:0,Discount=request.OrderType=="For Sale"?x.Discount:0});_db.Add(new InventoryStockTransaction{SchoolId=request.SchoolId,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,TransactionType="OUT",Quantity=x.Quantity,ReferenceType="StudentOrder",ReferenceId=order.Id});}
            await _db.SaveChangesAsync();await tx.CommitAsync();return Success(order,request.OrderType=="For Use"?"Temporary-use order created and stock updated.":"Sale order created and stock updated.");
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
        public async Task<IActionResult> Payment(InventoryPayment item){var o=await _db.InventoryStudentOrders.FindAsync(item.StudentOrderId);if(o==null)return NotFound(new{success=false,message="Order not found."});if(item.Amount<=0||item.Amount>o.TotalAmount-o.PaidAmount)return BadRequest(new{success=false,message="Payment must be positive and cannot exceed the outstanding balance."});item.ReceiptNumber=string.IsNullOrWhiteSpace(item.ReceiptNumber)?$"ST{DateTime.Now:yyyyMMddHHmmss}":item.ReceiptNumber;o.PaidAmount+=item.Amount;_db.Add(item);await _db.SaveChangesAsync();return Success(item,"Payment recorded.");}

        [HttpGet("returns")]
        public async Task<IActionResult> Returns(int schoolId)=>Success(await _db.InventoryReturns.Where(x=>x.SchoolId==schoolId).OrderByDescending(x=>x.ReturnDate).ToListAsync());
        [HttpPost("returns")]
        public async Task<IActionResult> Return(InventoryReturnRequest request)
        {
            var order=await _db.InventoryStudentOrders.FirstOrDefaultAsync(x=>x.Id==request.StudentOrderId&&x.SchoolId==request.SchoolId);
            if(order==null||order.Status!="Issued")return BadRequest(new{success=false,message="A valid issued order is required."});
            if(!request.Items.Any()||request.Items.Any(x=>x.Quantity<=0))return BadRequest(new{success=false,message="At least one positive return quantity is required."});
            foreach(var x in request.Items)
            {
                var issued=await _db.InventoryStudentOrderItems.Where(i=>i.StudentOrderId==order.Id&&i.ProductId==x.ProductId&&i.ProductVariantId==x.ProductVariantId).SumAsync(i=>(decimal?)i.Quantity)??0;
                var returned=await (from ri in _db.InventoryReturnItems join r in _db.InventoryReturns on ri.ReturnId equals r.Id where r.StudentOrderId==order.Id&&ri.ProductId==x.ProductId&&ri.ProductVariantId==x.ProductVariantId select (decimal?)ri.Quantity).SumAsync()??0;
                if(issued==0||returned+x.Quantity>issued)return Conflict(new{success=false,message="Return quantity exceeds the quantity issued for this product."});
            }
            await using var tx=await _db.Database.BeginTransactionAsync();
            var ret=new InventoryReturn{SchoolId=request.SchoolId,StudentOrderId=request.StudentOrderId,ReturnNumber=$"RT{DateTime.Now:yyyyMMddHHmmss}",Reason=request.Reason};
            _db.Add(ret);await _db.SaveChangesAsync();
            foreach(var x in request.Items){_db.Add(new InventoryReturnItem{ReturnId=ret.Id,ProductId=x.ProductId,ProductVariantId=x.ProductVariantId,Quantity=x.Quantity,Condition=x.Condition,Restock=x.Restock});if(x.Restock)await AddStock(request.SchoolId,x.ProductId,x.ProductVariantId,x.Quantity,"Return",ret.Id);}
            await _db.SaveChangesAsync();await tx.CommitAsync();return Success(ret,"Return recorded and eligible stock restored.");
        }

        [HttpGet("reports")]
        public async Task<IActionResult> Reports(int schoolId)=>Success(new{inventoryValue=await _db.InventoryProducts.Where(x=>x.SchoolId==schoolId).SumAsync(x=>x.CurrentStock*x.PurchasePrice),salesValue=await _db.InventoryStudentOrders.Where(x=>x.SchoolId==schoolId).SumAsync(x=>x.TotalAmount),collected=await _db.InventoryStudentOrders.Where(x=>x.SchoolId==schoolId).SumAsync(x=>x.PaidAmount),lowStock=await _db.InventoryProducts.CountAsync(x=>x.SchoolId==schoolId&&x.CurrentStock-x.ReservedStock<=x.MinimumStock)});

        private async Task<int> CountCategory(IQueryable<InventoryProduct> products,int schoolId,string term){var ids=await _db.InventoryCategories.Where(x=>x.SchoolId==schoolId&&x.Name.Contains(term)).Select(x=>x.Id).ToListAsync();return await products.CountAsync(x=>ids.Contains(x.CategoryId));}
        private async Task<IActionResult> SavePricedKit(InventoryKit item)
        {
            if (item.Mrp < 0 || item.SellingPrice < 0 || item.DiscountAmount < 0 || item.DiscountAmount > item.SellingPrice)
                return BadRequest(new { success=false, message="Kit prices must be non-negative and the discount cannot exceed the selling price." });

            _db.InventoryKits.Add(item);
            await _db.SaveChangesAsync();
            return Success(item, $"{item.KitType} kit created.");
        }

        private async Task<IActionResult> UpdatePricedKit(int id,InventoryKit input,string kitType)
        {
            if (input.Mrp < 0 || input.SellingPrice < 0 || input.DiscountAmount < 0 || input.DiscountAmount > input.SellingPrice)
                return BadRequest(new { success=false, message="Kit prices must be non-negative and the discount cannot exceed the selling price." });

            var existing=await _db.InventoryKits.FirstOrDefaultAsync(x=>x.Id==id&&x.KitType==kitType);
            if(existing==null)return NotFound(new{success=false,message=$"{kitType} kit not found."});
            input.Id=id;
            input.SchoolId=existing.SchoolId;
            input.KitType=kitType;
            _db.Entry(existing).CurrentValues.SetValues(input);
            await _db.SaveChangesAsync();
            return Success(existing,$"{kitType} kit updated.");
        }
        private async Task<IActionResult> UpdateMaster<TEntity>(DbSet<TEntity> set,int id,TEntity item,string label) where TEntity:class
        {
            var existing=await set.FindAsync(id);if(existing==null)return NotFound(new{success=false,message=$"{label} not found."});
            typeof(TEntity).GetProperty("Id")?.SetValue(item,id);
            var school=typeof(TEntity).GetProperty("SchoolId");if(school!=null)school.SetValue(item,school.GetValue(existing));
            _db.Entry(existing).CurrentValues.SetValues(item);await _db.SaveChangesAsync();return Success(existing,$"{label} updated.");
        }
        private async Task AddStock(int schoolId,int productId,int? variantId,decimal qty,string reference,int referenceId){var p=await _db.InventoryProducts.FindAsync(productId)??throw new InvalidOperationException("Product not found.");p.CurrentStock+=qty;if(variantId.HasValue){var v=await _db.InventoryProductVariants.FindAsync(variantId.Value)??throw new InvalidOperationException("Variant not found.");v.CurrentStock+=qty;}_db.Add(new InventoryStockTransaction{SchoolId=schoolId,ProductId=productId,ProductVariantId=variantId,TransactionType="IN",Quantity=qty,ReferenceType=reference,ReferenceId=referenceId});}
    }
}
