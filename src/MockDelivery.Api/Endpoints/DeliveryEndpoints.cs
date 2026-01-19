namespace MockDelivery.Api.Endpoints;

using Common.Models;
using MockDelivery.Api.Services;

public static class DeliveryEndpoints
{
    public static void MapDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/deliveries")
            .WithTags("Deliveries")
            .WithOpenApi();

        group.MapPost("/", CreateDelivery)
            .WithName("CreateDelivery")
            .WithSummary("Create a new delivery")
            .Produces<CreateDeliveryResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<CreateDeliveryResponse>(StatusCodes.Status200OK); // For idempotent requests

        group.MapGet("/{id}", GetDelivery)
            .WithName("GetDelivery")
            .WithSummary("Get delivery by ID")
            .Produces<DeliveryResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/cancel", CancelDelivery)
            .WithName("CancelDelivery")
            .WithSummary("Cancel a delivery")
            .Produces<CancelDeliveryResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateDelivery(
        [FromBody] CreateDeliveryRequest request,
        [FromServices] IDeliveryStore store,
        [FromServices] MockDeliverySettings settings,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        // Simulate random delay if enabled
        if (settings.Simulation.EnableRandomDelays)
        {
            var delay = Random.Shared.Next(0, settings.Simulation.MaxRandomDelayMs);
            await Task.Delay(delay, cancellationToken);
        }

        // Check idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = store.GetByIdempotencyKey(request.IdempotencyKey);
            if (existing != null)
            {
                logger.LogInformation(
                    "Idempotent request detected for key {IdempotencyKey}. Returning existing delivery {DeliveryId}",
                    request.IdempotencyKey,
                    existing.Id);

                return Results.Ok(new CreateDeliveryResponse
                {
                    DeliveryId = existing.Id,
                    Status = existing.Status,
                    CreatedAt = existing.CreatedAt,
                    Message = "Existing delivery returned (idempotent request)"
                });
            }
        }

        // Create new delivery
        var delivery = new Delivery
        {
            Id = Guid.NewGuid().ToString("N"),
            OrderId = request.OrderId,
            PickupAddress = request.PickupAddress,
            DeliveryAddress = request.DeliveryAddress,
            Status = DeliveryStatus.Created,
            IdempotencyKey = request.IdempotencyKey,
            WebhookUrl = request.WebhookUrl,
            CreatedAt = DateTime.UtcNow,
            Driver = new DeliveryDriver
            {
                Name = GenerateDriverName(),
                Phone = GeneratePhoneNumber(),
                VehicleNumber = GenerateVehicleNumber()
            }
        };

        // Mark for forced failure if requested
        if (request.ForceFail)
        {
            delivery.FailureReason = "FORCED_FAILURE_FOR_TESTING";
        }

        if (!store.TryAdd(delivery))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to create delivery");
        }

        logger.LogInformation(
            "Created delivery {DeliveryId} for order {OrderId}",
            delivery.Id,
            delivery.OrderId);

        return Results.Created(
            $"/api/v1/deliveries/{delivery.Id}",
            new CreateDeliveryResponse
            {
                DeliveryId = delivery.Id,
                Status = delivery.Status,
                CreatedAt = delivery.CreatedAt,
                Message = "Delivery created successfully"
            });
    }

    private static IResult GetDelivery(
        string id,
        [FromServices] IDeliveryStore store,
        [FromServices] MockDeliverySettings settings)
    {
        // Simulate random delay
        if (settings.Simulation.EnableRandomDelays)
        {
            var delay = Random.Shared.Next(0, settings.Simulation.MaxRandomDelayMs);
            Thread.Sleep(delay);
        }

        var delivery = store.GetById(id);
        if (delivery == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Delivery not found",
                Detail = $"No delivery found with ID: {id}",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Results.Ok(new DeliveryResponse
        {
            DeliveryId = delivery.Id,
            OrderId = delivery.OrderId,
            Status = delivery.Status,
            CreatedAt = delivery.CreatedAt,
            PickedUpAt = delivery.PickedUpAt,
            DeliveredAt = delivery.DeliveredAt,
            CancelledAt = delivery.CancelledAt,
            FailureReason = delivery.FailureReason,
            Driver = delivery.Driver,
            DeliveryAddress = delivery.DeliveryAddress
        });
    }

    private static IResult CancelDelivery(
        string id,
        [FromBody] CancelDeliveryRequest request,
        [FromServices] IDeliveryStore store,
        [FromServices] IWebhookService webhookService,
        [FromServices] ILogger<Program> logger)
    {
        var delivery = store.GetById(id);
        if (delivery == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Delivery not found",
                Detail = $"No delivery found with ID: {id}",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Can only cancel if not already in terminal state
        if (delivery.Status == DeliveryStatus.Delivered
            || delivery.Status == DeliveryStatus.Failed
            || delivery.Status == DeliveryStatus.Cancelled)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Cannot cancel delivery",
                Detail = $"Delivery is already in terminal state: {delivery.Status}",
                Status = StatusCodes.Status400BadRequest
            });
        }

        delivery.Status = DeliveryStatus.Cancelled;
        delivery.CancelledAt = DateTime.UtcNow;
        delivery.FailureReason = request.Reason ?? "Cancelled by user";

        store.TryUpdate(delivery);

        logger.LogInformation(
            "Cancelled delivery {DeliveryId}. Reason: {Reason}",
            delivery.Id,
            delivery.FailureReason);

        // Send webhook notification
        _ = Task.Run(() => webhookService.SendWebhookAsync(delivery));

        return Results.Ok(new CancelDeliveryResponse
        {
            DeliveryId = delivery.Id,
            Status = delivery.Status,
            CancelledAt = delivery.CancelledAt.Value,
            Message = "Delivery cancelled successfully"
        });
    }

    private static string GenerateDriverName()
    {
        // Use Bogus with South African locale for realistic names
        var faker = new Faker("en_ZA");
        return faker.Person.FullName;
    }

    private static string GeneratePhoneNumber()
    {
        // Generate South African phone numbers (+27 format)
        var faker = new Faker("en_ZA");
        // South African mobile format: +27 followed by 9 digits
        // Common prefixes: 60-89 (Vodacom, MTN, Cellfone, Virgin Mobile)
        return faker.Phone.PhoneNumber("+27 ## ### ####");
    }

    private static string GenerateVehicleNumber()
    {
        // Generate South African vehicle registration plates
        // Format: 2 letters + 2 digits + 3 letters
        var faker = new Faker();
        var letters = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Exclude I, O (confusing with 1, 0)
        var firstLetters = $"{letters[Random.Shared.Next(letters.Length)]}{letters[Random.Shared.Next(letters.Length)]}";
        var digits = Random.Shared.Next(10, 100).ToString("D2");
        var lastLetters = $"{letters[Random.Shared.Next(letters.Length)]}{letters[Random.Shared.Next(letters.Length)]}{letters[Random.Shared.Next(letters.Length)]}";
        return $"{firstLetters} {digits} {lastLetters}";
    }
}
