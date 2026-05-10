        using n3amco.Api;
        using n3amco.Api.Common;
        using Microsoft.AspNetCore.Authorization;
        using Microsoft.AspNetCore.Mvc;
        using Microsoft.EntityFrameworkCore;

        namespace n3amco.Api.Products
        {
            [ApiController]
            [Route("api/[controller]")]
            [Authorize]
            public class ProductsController : ControllerBase
            {
                private readonly ApplicationDbContext _context;
                private readonly ILogger<ProductsController> _logger;

                public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
                {
                    _context = context;
                    _logger = logger;
                }

                // ========================= CREATE =========================
                [HttpPost]
                [Authorize(Policy = "PRODUCTS_CREATE")]
                public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                        return BadRequest(ApiResponse<string>.Fail("Name is required"));

                    if (string.IsNullOrWhiteSpace(dto.Code))
                        return BadRequest(ApiResponse<string>.Fail("Code is required"));

                    if (await _context.Products.AnyAsync(x => x.Code == dto.Code))
                        return BadRequest(ApiResponse<string>.Fail("Code already exists"));

                    var unitExists = await _context.Units.AnyAsync(x => x.Id == dto.UnitId);
                    if (!unitExists)
                        return BadRequest(ApiResponse<string>.Fail("Invalid Unit"));

                    try
                    {
                        var product = new Product
                        {
                            Name = dto.Name,
                            Code = dto.Code,
                            Type = dto.Type,
                            UnitId = dto.UnitId,
                            DefaultPurchasePrice = dto.DefaultPurchasePrice,
                            DefaultSellingPrice = dto.DefaultSellingPrice,
                            IsRawMaterial = dto.IsRawMaterial,
                            IsManufactured = dto.IsManufactured,
                            MinStock = dto.MinStock,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        _context.Products.Add(product);

                        // 🔥 Opening Stock (FIFO Batch)
                        if (dto.OpeningQuantity.HasValue && dto.OpeningQuantity > 0)
                        {
                            _context.ProductBatches.Add(new ProductBatch
                            {
                                Product = product,
                                Quantity = dto.OpeningQuantity.Value,
                                RemainingQuantity = dto.OpeningQuantity.Value,
                                CostPrice = dto.DefaultPurchasePrice,
                                ExpiryDate = dto.OpeningExpiryDate, 
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        await _context.SaveChangesAsync();

                        return Ok(ApiResponse<int>.SuccessResponse(product.Id, "Product created"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Create Product Failed");
                        return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
                    }
                }

                // ========================= GET ALL =========================
                [HttpGet]
                [Authorize(Policy = "PRODUCTS_VIEW")]
                public async Task<IActionResult> GetAll([FromQuery] ProductQuery query)
                {
                    var products = _context.Products
                        .Include(x => x.Unit)
                        .Include(x => x.Batches)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(query.Search))
                    {
                        products = products.Where(x =>
                            x.Name.Contains(query.Search) ||
                            x.Code.Contains(query.Search));
                    }

                    if (query.IsActive.HasValue)
                    {
                        products = products.Where(x => x.IsActive == query.IsActive);
                    }

                    var total = await products.CountAsync();

                    var data = await products
                        .OrderByDescending(x => x.CreatedAt)
                        .Skip((query.Page - 1) * query.PageSize)
                        .Take(query.PageSize)
                        .Select(x => new
                        {
                            x.Id,
                            x.Name,
                            x.Code,
                            x.MinStock,
                            IsLowStock = x.Batches.Sum(b => (decimal?)b.RemainingQuantity) < x.MinStock,
                            Unit = x.Unit.Name,
                            Type = x.Type.ToString(),


                            Stock = x.Batches.Sum(b => (decimal?)b.RemainingQuantity) ?? 0,

                            x.DefaultPurchasePrice,
                            x.DefaultSellingPrice,
                            x.IsActive
                        })
                        .ToListAsync();

                    return Ok(ApiResponse<object>.SuccessResponse(new
                    {
                        total,
                        query.Page,
                        query.PageSize,
                        data
                    }));
                }

                // ========================= GET BY ID =========================
                [HttpGet("{id}")]
                [Authorize(Policy = "PRODUCTS_VIEW")]
                public async Task<IActionResult> Get(int id)
                {
                    var product = await _context.Products
                        .Include(x => x.Unit)
                        .Include(x => x.Batches)
                        .FirstOrDefaultAsync(x => x.Id == id);

                    if (product == null)
                        return NotFound(ApiResponse<string>.Fail("Product not found"));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                product.Id,
                product.Name,
                product.Code,
                Unit = product.Unit.Name,
                Type = product.Type.ToString(),
                product.DefaultPurchasePrice,
                product.DefaultSellingPrice,

                Stock = product.Batches.Sum(b => b.RemainingQuantity),

                Batches = product.Batches
    .OrderBy(b => b.ExpiryDate)
    .Select(b => new
    {
                    b.Id,
                    b.Quantity,
                    b.RemainingQuantity,
                    b.CostPrice,
                    b.ExpiryDate,
                    b.CreatedAt,
                    IsExpired = b.ExpiryDate != null && b.ExpiryDate < DateTime.UtcNow,
                    IsNearExpiry = b.ExpiryDate != null && b.ExpiryDate <= DateTime.UtcNow.AddDays(7)
                })
            }));
        }

                // ========================= UPDATE =========================
                [HttpPut("{id}")]
                [Authorize(Policy = "PRODUCTS_UPDATE")]
                public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
                {
                    var product = await _context.Products.FindAsync(id);

                    if (product == null)
                        return NotFound(ApiResponse<string>.Fail("Product not found"));

                    if (await _context.Products.AnyAsync(x => x.Code == dto.Code && x.Id != id))
                        return BadRequest(ApiResponse<string>.Fail("Code already exists"));

                    product.Name = dto.Name;
                    product.Code = dto.Code;
                    product.UnitId = dto.UnitId;
                    product.DefaultPurchasePrice = dto.DefaultPurchasePrice;
                    product.DefaultSellingPrice = dto.DefaultSellingPrice;
                    product.MinStock = dto.MinStock; 

                    await _context.SaveChangesAsync();

                    return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
                }

                // ========================= DELETE =========================
                [HttpDelete("{id}")]
                [Authorize(Policy = "PRODUCTS_DELETE")]
                public async Task<IActionResult> Delete(int id)
                {
                    var product = await _context.Products.FindAsync(id);

                    if (product == null)
                        return NotFound(ApiResponse<string>.Fail("Product not found"));

                    product.IsActive = false;

                    await _context.SaveChangesAsync();

                    return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
                }

                // ========================= STOCK =========================
                [HttpGet("{id}/stock")]
                [Authorize(Policy = "PRODUCTS_VIEW")]
                public async Task<IActionResult> GetStock(int id)
                {
                    var product = await _context.Products
                        .Include(x => x.Batches)
                        .FirstOrDefaultAsync(x => x.Id == id);

                    if (product == null)
                        return NotFound(ApiResponse<string>.Fail("Product not found"));

                    var stock = product.Batches.Sum(x => x.RemainingQuantity);

                    return Ok(ApiResponse<decimal>.SuccessResponse(stock));
                }
                // ========================= STOCK ADJUSTMENT =========================
                [HttpPost("{id}/adjust")]
                [Authorize(Policy = "PRODUCTS_UPDATE")]
                public async Task<IActionResult> Adjust(int id, [FromBody] StockAdjustmentDto dto)
                {
                    if (dto.Quantity <= 0)
                        return BadRequest(ApiResponse<string>.Fail("الكمية يجب أن تكون أكبر من صفر"));

                    if (string.IsNullOrWhiteSpace(dto.Reason))
                        return BadRequest(ApiResponse<string>.Fail("سبب التسوية مطلوب"));

                    var product = await _context.Products
                        .Include(x => x.Batches)
                        .FirstOrDefaultAsync(x => x.Id == id);

                    if (product == null)
                        return NotFound(ApiResponse<string>.Fail("المنتج غير موجود"));

                    // لو خصم — تأكد إن المخزون كافي
                    if (dto.Type == AdjustmentType.Remove)
                    {
                        var currentStock = product.Batches.Sum(b => b.RemainingQuantity);
                        if (dto.Quantity > currentStock)
                            return BadRequest(ApiResponse<string>.Fail($"المخزون الحالي {currentStock} أقل من الكمية المطلوبة"));
                    }

                    var createdBy = User.Identity?.Name ?? "system";

                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // سجل التسوية
                        var adjustment = new StockAdjustment
                        {
                            ProductId = id,
                            Quantity = dto.Type == AdjustmentType.Add ? dto.Quantity : -dto.Quantity,
                            Type = dto.Type,
                            Reason = dto.Reason,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = createdBy
                        };
                        _context.StockAdjustments.Add(adjustment);

                        if (dto.Type == AdjustmentType.Add)
                        {
                            // إضافة Batch جديد بسعر تكلفة آخر باتش
                            var lastCost = product.Batches
                                .OrderByDescending(b => b.CreatedAt)
                                .FirstOrDefault()?.CostPrice ?? 0;

                            _context.ProductBatches.Add(new ProductBatch
                            {
                                ProductId = id,
                                Quantity = dto.Quantity,
                                RemainingQuantity = dto.Quantity,
                                CostPrice = lastCost,
                                ExpiryDate = dto.ExpiryDate,
                                SourceType = BatchSourceType.Adjustment,
                                ReferenceId = 0,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            // خصم بنظام FIFO من أقدم الباتشات
                            var remaining = dto.Quantity;
                            var batches = product.Batches
                                .Where(b => b.RemainingQuantity > 0)
                                .OrderBy(b => b.CreatedAt)
                                .ToList();

                            foreach (var batch in batches)
                            {
                                if (remaining <= 0) break;
                                var take = Math.Min(batch.RemainingQuantity, remaining);
                                batch.RemainingQuantity -= take;
                                remaining -= take;
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Ok(ApiResponse<string>.SuccessResponse("تمت التسوية بنجاح"));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Stock Adjustment Failed");
                        return StatusCode(500, ApiResponse<string>.Fail("حدث خطأ أثناء التسوية"));
                    }
                }
                // ========================= MOVEMENTS =========================
                [HttpGet("{id}/movements")]
                [Authorize(Policy = "PRODUCTS_VIEW")]
                public async Task<IActionResult> GetMovements(int id, int page = 1, int pageSize = 15)
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                        return NotFound(ApiResponse<string>.Fail("المنتج غير موجود"));

                    var movements = new List<object>();

                    // 1. وارد — من الباتشات (شراء / إنتاج / opening stock)
                    var batches = await _context.ProductBatches
                        .Where(b => b.ProductId == id)
                        .Select(b => new {
                            date = b.CreatedAt,
                            type = "وارد",
                            source = b.SourceType.ToString(),
                            quantity = b.Quantity,
                            cost = b.CostPrice,
                            notes = $"باتش #{b.Id}"
                        }).ToListAsync();

                    // 2. صادر — من السيلز
                    var sales = await _context.SaleItems
                        .Where(s => s.ProductId == id)
                        .Include(s => s.Sale)
                        .Select(s => new {
                            date = s.Sale.CreatedAt,
                            type = "صادر",
                            source = "بيع",
                            quantity = -s.Quantity,
                            cost = s.CostPrice,
                            notes = $"فاتورة #{s.SaleId}"
                        }).ToListAsync();

                    // 3. تسويات
                    var adjustments = await _context.StockAdjustments
                        .Where(a => a.ProductId == id)
                        .Select(a => new {
                            date = a.CreatedAt,
                            type = a.Type == AdjustmentType.Add ? "تسوية +" : "تسوية -",
                            source = "تسوية مخزون",
                            quantity = a.Quantity,
                            cost = (decimal)0,
                            notes = a.Reason
                        }).ToListAsync();

                    // دمج وترتيب
                    movements.AddRange(batches);
                    movements.AddRange(sales);
                    movements.AddRange(adjustments);

                    var sorted = movements
                        .OrderByDescending(m => ((dynamic)m).date)
                        .ToList();

                    var total = sorted.Count;
                    var data = sorted
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    return Ok(ApiResponse<object>.SuccessResponse(new { total, page, pageSize, data }));
                }
        [HttpPost("{id}/batches")]
        [Authorize(Policy = "PRODUCTS_ADD_BATCH")]
        public async Task<IActionResult> AddBatch(int id, [FromBody] AddBatchDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest(ApiResponse<string>.Fail("الكمية لازم تكون أكبر من صفر"));

            if (dto.CostPrice < 0)
                return BadRequest(ApiResponse<string>.Fail("سعر التكلفة غير صحيح"));

            var product = await _context.Products
                .Include(p => p.Batches)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(ApiResponse<string>.Fail("المنتج غير موجود"));

            var createdBy = User.Identity?.Name ?? "system";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var batch = new ProductBatch
                {
                    ProductId = id,
                    Quantity = dto.Quantity,
                    RemainingQuantity = dto.Quantity,
                    CostPrice = dto.CostPrice,
                    ExpiryDate = dto.ExpiryDate,
                    SourceType = BatchSourceType.Purchase, // 👈 مهم
                    ReferenceId = dto.ReferenceId ?? 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductBatches.Add(batch);

                // 🔥 optional logging (زي adjustments)
                _context.StockAdjustments.Add(new StockAdjustment
                {
                    ProductId = id,
                    Quantity = dto.Quantity,
                    Type = AdjustmentType.Add,
                    Reason = "Add Batch",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    batch.Id,
                    batch.Quantity,
                    batch.ExpiryDate
                }, "تم إضافة الباتش بنجاح"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Add Batch Failed");

                return StatusCode(500, ApiResponse<string>.Fail("حصل خطأ"));
            }
        }
        [HttpGet("{id}/batches")]
        [Authorize(Policy = "PRODUCTS_VIEW")]
        public async Task<IActionResult> GetBatches(int id)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == id);
            if (!productExists)
                return NotFound(ApiResponse<string>.Fail("المنتج غير موجود"));

            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == id)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new
                {
                    b.Id,
                    b.Quantity,
                    b.RemainingQuantity,
                    b.CostPrice,
                    b.ExpiryDate,
                    b.CreatedAt,

                    IsExpired = b.ExpiryDate != null && b.ExpiryDate < DateTime.UtcNow,
                    IsNearExpiry = b.ExpiryDate != null && b.ExpiryDate <= DateTime.UtcNow.AddDays(7)
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(batches));
        }
    }

        }