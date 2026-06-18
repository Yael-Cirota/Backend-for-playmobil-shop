# Backend for Playmobil Shop

ASP.NET Core Web API backend for the Playmobil shop application.

## Project Overview

This backend provides APIs for:

- Users and authentication-related flows
- Products and categories
- Orders
- Password strength scoring
- Image upload
- AI-generated custom box covers
- Semantic product search (AI embeddings)
- AI chat assistant responses

The project uses a layered architecture (Controllers -> Services -> Repositories -> EF Core).

## Architecture

Solution structure:

- WebApiShop: API host, controllers, middleware, startup and configuration
- Services: business logic layer
- Repositories: data access layer with EF Core
- Entities: domain models
- DTOs: request and response contracts
- TestProject: unit and integration tests

High-level request flow:

1. Client (Angular app) sends HTTP request to a controller.
2. Controller validates request and delegates to a service.
3. Service applies business rules and uses repositories.
4. Repository reads/writes database through ApiDBContext.
5. Service maps results to DTOs and returns to controller.
6. Controller returns API response to client.

## Tech Stack

- .NET 9 (Web API)
- Entity Framework Core (SQL Server)
- AutoMapper
- NLog
- OpenAPI/Swagger
- ImageSharp (cover image processing)

## Prerequisites

- .NET SDK 9.0+
- SQL Server instance
- OpenAI API key (for cover generation endpoint)
- Python 3.11+ (for the local AI service used by semantic search)
- Python packages for `ai_service` (FastAPI, Uvicorn, OpenAI, NumPy, etc.)

## Getting Started

From the backend root folder:

```bash
dotnet restore
dotnet build
dotnet run --project WebApiShop
```

### Semantic Search Dependency (Required For `POST api/search`)

The backend `SearchService` calls a local Python service at:

- `http://localhost:8010/search`

Start the Python AI service from `ai_service` before testing `POST api/search`:

```bash
cd ../ai_service
python -m uvicorn chat_service:app --reload --port 8010
```

If you use a virtual environment, activate it first and ensure dependencies are installed.

### Chat Dependency (Required For `POST api/chat`)

The backend `ChatService` calls the same local Python AI service at:

- `http://localhost:8010/chat`

So the same `ai_service` process must be running before testing chat endpoints.

Default development URLs are configured in launch settings:

- https://localhost:44313
- http://localhost:5267

Swagger UI is available in Development.

## Configuration

Main configuration file:

- WebApiShop/appsettings.json

Required settings:

- ConnectionStrings: set SQL Server connection string used by the API
- OpenAI:ApiToken: required for POST api/covers/generate

Recommended secret handling:

- Use environment variables or user secrets for sensitive values
- Do not commit API keys or other secrets to source control

Example environment variable names:

- ConnectionStrings__Yael
- OpenAI__ApiToken

## CORS

The API currently allows these frontend origins:

- http://localhost:4200
- http://localhost:53883

If your frontend runs on a different origin, update the CORS policy in WebApiShop/Program.cs.

## API Endpoints Summary

Base route prefix:

- api

Main controllers:

- GET api/categories
- GET api/products
- GET api/products/{id}
- POST api/products
- PUT api/products/{id}
- GET api/orders
- GET api/orders/{id}
- POST api/orders
- PUT api/orders/{id}
- GET api/users
- GET api/users/{id}
- POST api/users
- POST api/users/login
- PUT api/users/{id}
- GET api/users/{id}/orders
- POST api/passwords/passwordscore
- POST api/upload/upload
- POST api/covers/generate
- POST api/chat
- POST api/search

## Chat Notes

- `POST api/chat` receives a chat payload with `message`, optional `history`, and optional `products`.
- Backend forwards this request to the Python AI service (`/chat`).
- The response contains:
	- `reply` (assistant text)
	- `products` (optional recommended product cards)

### Common Error: Chat service unreachable (`localhost:8010`)

If Swagger returns an error for `POST api/chat` indicating the AI service cannot be reached:

1. Verify the Python service is running from `ai_service` on port `8010`.
2. Verify no other process is occupying port `8010`.
3. Verify local firewall/antivirus is not blocking Python localhost connections.
4. Restart both services in this order: Python AI service, then ASP.NET backend.

## Semantic Search Notes

- `POST api/search` receives `{ query: string }`.
- Backend maps products and forwards them to the Python AI service (`/search`).
- AI service returns ranked results under `results`.

### Common Error: Cannot reach search service / socket permissions (`localhost:8010`)

If Swagger returns an error like:

- `Cannot reach search service: An attempt was made to access a socket in a way forbidden by its access permissions. (localhost:8010)`

Check:

1. The Python service is actually running and listening on port `8010`.
2. No other process is blocking port `8010`.
3. Local firewall/endpoint security is not blocking Python on localhost.
4. The backend and Python service are both started from the correct folders.

## Cover Generation Notes

The cover endpoint expects a request with:

- imageBase64
- productName
- boxImageUrl
- orderId and productId (as provided by the frontend flow)

Current response shape:

- coverUrls: array of image data URIs (data:image/png;base64,...)

## Tests

Run tests from backend root:

```bash
dotnet test
```

## Related Frontend

Frontend repository:

- https://github.com/Yael-Cirota/playmobil-shop-angular