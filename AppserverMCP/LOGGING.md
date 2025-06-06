# Request Logging in AppserverMCP

This document explains how to log all requests coming into the MCP server.

## 1. Built-in ASP.NET Core HTTP Logging (Default)

The application is configured with ASP.NET Core's built-in HTTP logging middleware which logs:

- Request and response headers
- Request and response bodies (for text content)
- Request timing
- Status codes

### Configuration

The logging is configured in `Program.cs`:

```csharp
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.RequestHeaders.Add("User-Agent");
    logging.RequestHeaders.Add("A4SAuthorization");
    logging.RequestHeaders.Add("ROPC");
    logging.MediaTypeOptions.AddText("application/json");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});
```

### Log Level Configuration

Set the log level in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.HttpLogging": "Information"
    }
  }
}
```

### Sample Log Output

```
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[1]
      Request:
      Protocol: HTTP/1.1
      Method: GET
      Scheme: http
      PathBase: 
      Path: /
      Accept: */*
      User-Agent: curl/7.68.0
      Host: localhost:3001
```

## 2. Custom Detailed Request Logging (Optional)

For more detailed logging with structured JSON output, you can enable the custom middleware.

### Enable Custom Logging

Uncomment this line in `Program.cs`:

```csharp
// Change this:
// app.UseDetailedRequestLogging();

// To this:
app.UseDetailedRequestLogging();
```

**Note:** Use either the built-in logging OR the custom logging, not both.

### Features

- 🔵 Request start with structured JSON
- 🟢 Success responses (200-299)
- 🟡 Redirect responses (300-399)
- 🟠 Client error responses (400-499)
- 🔴 Server error responses (500+)
- Request ID tracking
- Execution timing
- Client IP detection
- Automatic body filtering (excludes binary/large content)

### Sample Custom Log Output

```json
🔵 REQUEST START: {
  "RequestId": "a1b2c3d4",
  "Timestamp": "2023-12-07T10:30:00.000Z",
  "Method": "GET",
  "Path": "/",
  "QueryString": "",
  "ContentType": null,
  "ContentLength": null,
  "Headers": {
    "User-Agent": "curl/7.68.0",
    "Host": "localhost:3001"
  },
  "Body": "[BODY_NOT_LOGGED]",
  "ClientIP": "127.0.0.1",
  "UserAgent": "curl/7.68.0"
}

🟢 RESPONSE END: {
  "RequestId": "a1b2c3d4",
  "Timestamp": "2023-12-07T10:30:00.123Z",
  "StatusCode": 200,
  "StatusDescription": "OK",
  "ContentType": "application/json; charset=utf-8",
  "ContentLength": 234,
  "Headers": {
    "Content-Type": "application/json; charset=utf-8"
  },
  "Body": "{\"message\":\"Welcome to AppserverMCP API\",...}",
  "ElapsedMs": 123
}
```

## 3. Log Level Configuration

### For Production (Minimal Logging)

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.HttpLogging": "Warning",
      "AppserverMCP.Middleware.RequestLoggingMiddleware": "Warning"
    }
  }
}
```

### For Development (Verbose Logging)

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.HttpLogging": "Information",
      "AppserverMCP.Middleware.RequestLoggingMiddleware": "Information"
    }
  }
}
```

### For Debugging (All Logs)

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.HttpLogging": "Debug",
      "AppserverMCP.Middleware.RequestLoggingMiddleware": "Debug"
    }
  }
}
```

## 4. Available Endpoints

When you run the server on `http://localhost:3001`, the following endpoints will be available and logged:

### Root Endpoints
- `GET /` - Welcome message and endpoint list
- `GET /about` - Appserver information
- `GET /models` - Models list
- `GET /users` - Users list
- `GET /tasks` - Tasks list
- `GET /business-processes` - Business processes
- `GET /server-status` - Server status
- `GET /model-statistics` - Model statistics
- `GET /license` - License information

### API Endpoints
- `POST /api/tasks/{taskId}/execute` - Execute a task
- `GET /api/tasks/{taskId}/status` - Get task status
- `GET /api/models/{modelId}/classes` - Get model classes
- `PUT /api/users/{userUri}/currency` - Update user currency
- `GET /api/users/{userUri}/settings` - Get user settings

### Health Endpoints
- `GET /api/health` - Basic health check
- `GET /api/health/detailed` - Detailed health with backend checks
- `GET /api/health/ready` - Readiness probe
- `GET /api/health/live` - Liveness probe

## 5. Running the Server

To start the server with logging:

```bash
cd AppserverMCP
dotnet run
```

The server will start on `http://localhost:3001` and all requests will be logged according to your configuration.

## 6. Testing the Logging

You can test the endpoints with curl:

```bash
# Test root endpoint
curl http://localhost:3001/

# Test health endpoint
curl http://localhost:3001/api/health

# Test models endpoint
curl http://localhost:3001/models
```

All these requests will be logged with the configured detail level. 