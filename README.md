# AppserverMCP - Model Context Protocol Server

## Overview

AppserverMCP is a Model Context Protocol (MCP) server implementation built with .NET 9.0 that provides HTTP/SSE-based communication for interacting with your Appserver. This implementation is inspired by the MonkeyMCPSSE project and provides tools to query Appserver information and model status.

## Features

### Core Components

* **MCP Server**: Built using the ModelContextProtocol library (version 0.1.0-preview.2)
* **HTTP/SSE Transport**: Uses Server-Sent Events over HTTP for communication with clients
* **Appserver Integration**: Connects to your Appserver to retrieve information and model status

### Services

* **AppserverService**: A service that communicates with your Appserver API
  * Fetches comprehensive server information from the `/about` endpoint
  * Provides methods to retrieve model information and status
  * Implements caching for better performance (5-minute cache timeout)

### Available Tools

The server exposes several MCP tools that can be invoked by clients:

#### Appserver Tools

* **GetAppserverAbout**: Returns comprehensive information about the Appserver including version and all available models with their status
* **GetModelInfo**: Retrieves information about a specific model by its model ID
* **GetAllModels**: Gets a list of all available models from the Appserver
* **GetModelsStatus**: Gets the current status of all models (Up/Down) with their last update timestamps
#### Task Management Tools

* **ExecuteTask**: Execute a task by ID with optional reason parameter
* **GetTaskStatus**: Get current status and details of a specific task by ID
* **GetTasks**: Get list of all tasks from the Appserver

#### System Information Tools

* **GetSystemLicense**: Get system license information from the Appserver (`system/license` endpoint)

## Configuration

### Appserver Connection

Configure your Appserver base URL in `appsettings.json`:

```json
{
  "AppserverBaseUrl": "http://localhost:8080"
}
```

You can also set this via environment variables:
```bash
export AppserverBaseUrl="http://your-appserver:8080"
```

## Getting Started

### Prerequisites

* .NET 9.0 SDK or later
* Access to your Appserver with the `/about` endpoint available

### Building and Running

1. **Clone/Navigate to the project directory**
2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
3. **Build the project**:
   ```bash
   dotnet build
   ```
4. **Run the server**:
   ```bash
   dotnet run --project AppserverMCP
   ```

The server will start on `http://localhost:3001` by default.

### Using with MCP Clients

#### VS Code with GitHub Copilot

Add this configuration to your VS Code settings:

```json
{
  "mcp": {
    "servers": {
      "appservermcp": {
        "command": "curl",
        "args": [
          "-N",
          "-H", "Accept: text/event-stream",
          "http://localhost:3001/mcp"
        ],
        "env": {}
      }
    }
  }
}
```

#### Claude Desktop

Add this to your Claude Desktop MCP configuration:

```json
{
  "servers": {
    "appservermcp": {
      "command": "curl",
      "args": [
        "-N",
        "-H", "Accept: text/event-stream", 
        "http://localhost:3001/mcp"
      ],
      "env": {}
    }
  }
}
```

#### Using MCP Inspector

For testing and development, you can use the MCP Inspector:

```bash
npx @modelcontextprotocol/inspector http://localhost:3001/mcp
```

## API Endpoints

### Health Check Endpoints

* **GET /**: Returns a simple status message
* **GET /health**: Returns a health status with timestamp

### MCP Endpoint

* **GET /mcp**: The main MCP endpoint that serves Server-Sent Events

## Expected Appserver Response Format

Your Appserver's `/about` endpoint should return JSON in this format:

```json
{
    "app_server_version": "9999.999.99.6098",
    "models": [
        {
            "model_id": "EA2_800",
            "version": "25.99.0.30",
            "status": "Up",
            "modeldata_timestamp": 1748914920,
            "model_definition_version": 353,
            "is_real_time": false
        },
        {
            "model_id": "EA5_800",
            "version": "25.99.0.30",
            "status": "Up",
            "modeldata_timestamp": 1748997286,
            "model_definition_version": 291,
            "is_real_time": false
        }
    ]
}
```

## Project Structure

```
AppserverMCP/
├── AppserverMCP.csproj          # Project file with dependencies
├── Program.cs                   # Entry point and web server configuration
├── AppserverService.cs          # Service for communicating with Appserver
├── AppserverTools.cs            # MCP tools implementation
├── appsettings.json             # Application configuration
├── appsettings.Development.json # Development-specific configuration
└── README.md                    # This file
```

## Dependencies

* **Microsoft.Extensions.Hosting** (9.0.3): Provides hosting infrastructure
* **Microsoft.Extensions.Logging.*** (9.0.3): Logging providers
* **ModelContextProtocol** (0.1.0-preview.2): MCP server implementation
* **System.Text.Json** (9.0.3): JSON serialization/deserialization

## Troubleshooting

### Connection Issues

1. **Verify Appserver is running**: Make sure your Appserver is accessible at the configured URL
2. **Check network connectivity**: Ensure the MCP server can reach your Appserver
3. **Review logs**: Check the console output for detailed error messages

### Configuration Issues

1. **Verify AppserverBaseUrl**: Make sure the URL in your configuration points to your Appserver
2. **Check port availability**: Ensure port 3001 is available for the MCP server

## License

This project is available under the MIT License.

## Contributing

Feel free to submit issues and pull requests to improve the functionality and add new features. 