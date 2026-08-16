/* Additive migration for the Inventory and Transport change requirements.
   Existing purchase, quantity, stop, GPS, capacity, and fee data is retained. */

IF COL_LENGTH('InventoryStudentOrders', 'OrderType') IS NULL
    ALTER TABLE InventoryStudentOrders ADD OrderType NVARCHAR(20) NOT NULL CONSTRAINT DF_InventoryOrders_OrderType DEFAULT 'For Sale';
IF COL_LENGTH('InventoryStudentOrders', 'BorrowDateTime') IS NULL
    ALTER TABLE InventoryStudentOrders ADD BorrowDateTime DATETIME2 NULL;
IF COL_LENGTH('InventoryStudentOrders', 'ReturnDateTime') IS NULL
    ALTER TABLE InventoryStudentOrders ADD ReturnDateTime DATETIME2 NULL;

IF COL_LENGTH('StudentTransportAllocations', 'PickupStop') IS NULL
    ALTER TABLE StudentTransportAllocations ADD PickupStop NVARCHAR(200) NULL;
IF COL_LENGTH('StudentTransportAllocations', 'DropStop') IS NULL
    ALTER TABLE StudentTransportAllocations ADD DropStop NVARCHAR(200) NULL;
IF COL_LENGTH('StudentTransportAllocations', 'FeeType') IS NULL
    ALTER TABLE StudentTransportAllocations ADD FeeType NVARCHAR(30) NOT NULL CONSTRAINT DF_TransportAllocation_FeeType DEFAULT 'Monthly';
IF COL_LENGTH('StudentTransportAllocations', 'DueDate') IS NULL
    ALTER TABLE StudentTransportAllocations ADD DueDate DATETIME2 NULL;

/* Preserve old stop IDs and backfill readable manual stop names for existing assignments. */
EXEC sys.sp_executesql N'
UPDATE a SET PickupStop = s.StopName
FROM StudentTransportAllocations a JOIN TransportRouteStops s ON s.Id = a.PickupStopId
WHERE a.PickupStop IS NULL;';
EXEC sys.sp_executesql N'
UPDATE a SET DropStop = s.StopName
FROM StudentTransportAllocations a JOIN TransportRouteStops s ON s.Id = a.DropStopId
WHERE a.DropStop IS NULL;';

/* New assignments use manual stop text; legacy stop references remain available. */
ALTER TABLE StudentTransportAllocations ALTER COLUMN PickupStopId INT NULL;
ALTER TABLE StudentTransportAllocations ALTER COLUMN DropStopId INT NULL;

IF COL_LENGTH('TransportFuelLogs', 'PaymentMode') IS NULL
    ALTER TABLE TransportFuelLogs ADD PaymentMode NVARCHAR(30) NULL;
IF COL_LENGTH('TransportFuelLogs', 'PaidTo') IS NULL
    ALTER TABLE TransportFuelLogs ADD PaidTo NVARCHAR(200) NULL;
IF COL_LENGTH('TransportVehicleMaintenance', 'BillAttachmentUrl') IS NULL
    ALTER TABLE TransportVehicleMaintenance ADD BillAttachmentUrl NVARCHAR(500) NULL;
