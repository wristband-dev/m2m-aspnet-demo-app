<div align="center">
  <a href="https://wristband.dev">
    <picture>
      <img src="https://assets.wristband.dev/images/email_branding_logo_v1.png" alt="Github" width="297" height="64">
    </picture>
  </a>
  <p align="center">
    Enterprise-ready auth that is secure by default, truly multi-tenant, and ungated for small businesses.
  </p>
  <p align="center">
    <b>
      <a href="https://wristband.dev">Website</a> • 
      <a href="https://docs.wristband.dev/">Documentation</a>
    </b>
  </p>
</div>

<br/>

---

<br/>

# Wristband Machine-to-Machine Demo Server (ASP.NET)

This is a C# Server that demonstrates the following:
- How to acquire an access token on server startup for a machine-to-machine (M2M) OAuth2 client
- How to protect an API with access tokens
- How to refresh the access tokens for the M2M OAuth2 client.

<br/>
<br>
<hr />

## Getting Started

You can start up the M2M demo application in a few simple steps.

### 1) Sign up for a Wristband account.

First, make sure you sign up for a Wristband account at [https://wristband.dev](https://wristband.dev).

### 2) Provision the .NET/C# demo application in the Wristband Dashboard.

After your Wristband account is set up, log in to the Wristband dashboard.  Once you land on the home page of the dashboard, click the button labelled "Add Demo App".  Make sure you choose the following options:

- Step 1: Subject to Authenticate - Machines
- Step 2: Client Framework - ASP.NET / C#
- Step 3: Domain Format  - Only `Localhost` is supported for M2M demo applications.

You can also follow the [Demo App Guide](https://docs.wristband.dev/docs/setting-up-a-demo-app) for more information.

### 3) Apply your Wristband configuration values to the C# server configuration

After completing demo app creation, you will be prompted with values that you should use to create environment variables for the C# server. You should see:

- `APPLICATION_DOMAIN`
- `CLIENT_ID`
- `CLIENT_SECRET`

Copy those values, then create an environment variable file on the server at: `<project_root_dir>/demo/.env`. Once created, paste the copied values into this file.

### 4) Run the application

> [!WARNING]
> Make sure you are in the root directory of this repository.

Make sure all dependecnies are installed:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

The C# backend is exposed on port `6001`. You can run with the following:

```bash
dotnet run --project ./demo
```

For development or debugging, you can run the server in watch mode:

```bash
dotnet watch --project ./demo run
```

## Demo App Overview

Below is a quick overview of this M2M Client demo server and how it interacts with Wristband.

### Entity Model

The entity model starts at the top with an application.  The application has one M2M OAuth2 client through which the server will be authenticated.  In this case, the client is a C# server.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://assets.wristband.dev/docs/GitHub+READMEs/m2m-demo-app/common/m2m-demo-app-entity-model-dark.png">
  <source media="(prefers-color-scheme: light)" srcset="https://assets.wristband.dev/docs/GitHub+READMEs/m2m-demo-app/common/m2m-demo-app-entity-model-light.png">
  <img alt="entity model" src="https://assets.wristband.dev/docs/GitHub+READMEs/m2m-demo-app/common/m2m-demo-app-entity-model-light.png">
</picture>

### Architecture

The demo server consists of two REST APIs: one that can be called without the need for an access token, and another that always requires a valid access token in the request headers.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://assets.wristband.dev/docs/GitHub+READMEs/m2m-demo-app/common/m2m-demo-app-architecture-dark.png">
  <source media="(prefers-color-scheme: light)" srcset="https://assets.wristband.dev/docs/GitHub+READMEs/m2m-demo-app/common/m2m-demo-app-architecture-light.png">
  <img alt="entity model" src="https://assets.wristband.dev/docs/GitHub+READMEs/m2m-demo-app/common/m2m-demo-app-architecture-light.png">
</picture>


## Demo Server Endpoints

Part of the server-startup process includes making a call to Wristband's Token Endpoint to acquire an access token for this server using the Client Credentials grant type.  It will store the access token and expiration time in a cache.

You will interact with the server by calling the public data API.

### Public Data API

`GET http://localhost:6001/api/public/data`

This is the endpoint you can hit from any command line or API testing tool (cURL, Postman, etc.) without passing any access token.  When a request is sent to this API, the API will turn around and make an API call to the protected data API with the access token that was acquired during server startup.  This is to simulate something akin to a microservices environment where an upstream service would be responsible for sending an acess token with every downstream request. The expected response output of this API is:

`"Public API called Protected API and received: \"Hello from Protected API!\""`

### Protected Data API

`GET http://localhost:6001/api/protected/data`

This endpoint is the downstream API called by the public data API, and it cannot be called without a valid access token.  When this API is invoked, the `Authorization` middleware to validates the following:
- The signature is valid when using the public keys as obtained from the Wristband JWKS endpoint.
- The access token is not expired.
- The issuer matches your Wristband application domain.
- The RS256 algorithm is specified.

## Getting New Access Tokens

An instance of `WristbandM2MAuth` is injecteded into the `ProtectedApiClient`, which the Public API uses for making requests to the Protected API.  With each request made to the Protected API, the code logic uses `WristbandM2MAuth` to get the access from the local memory cache as long as it exists and is not expired.  If both conditions are met, it will stick the access token in the Authorization header automatically.  Otherwise, it won't proceed with the original downstream request until an attempt to get a new access token is complete.  Wristband's Token Endpoint gets called with the Client Credentials grant type to get a new token, and the new token will be saved to local memory cache along with the new expiration time.

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions regarding this demo app.

<br/>
