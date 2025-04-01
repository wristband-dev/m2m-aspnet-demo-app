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

# Wristband Machine-to-Machine OAuth Client Demo Server (ASP.NET)

This is a C# Server that demonstrates the following:
- How to acquire an access token on server startup for a machine-to-machine (M2M) OAuth client
- How to protect an API with access tokens
- How to refresh the access tokens for the M2M OAuth2 client.

<br/>
<br>
<hr />

## Create Your OAuth Client in Wristband

In your Wristband account, create a new application. You can put any value desired for the application's `loginUrl` since it is arbitrary in the absence of any user-facing interaction. Next, register a M2M OAuth2 Client for that application.  Copy the `clientId` and `clientSecret` as well as the application's vanity domain.  You will need those values to configure environment variables for this server.

## Setup and Start the Server

From the root of this project, paste the values for application vanity domain, client ID, and client secret into the `.env` file.  Next, install all server dependencies required by the server by running the command `dotnet restore`. Finally, run the `dotnet build` command to start the server.  The server will start on port 6001.

## Demo App Overview

Below is a quick overview of this M2M Client demo server and how it interacts with Wristband.

### Entity Model
<br>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://assets.wristband.dev/docs/m2m-client-expressjs-demo-app/m2m-client-expressjs-demo-app-entity-model-dark.png">
  <source media="(prefers-color-scheme: light)" srcset="https://assets.wristband.dev/docs/m2m-client-expressjs-demo-app/m2m-client-expressjs-demo-app-entity-model-light.png">
  <img alt="entity model" src="https://assets.wristband.dev/docs/m2m-client-expressjs-demo-app/m2m-client-expressjs-demo-app-entity-model-light.png">
</picture>

The entity model starts at the top with an application.  The application has one M2M OAuth2 client through which the server will be authenticated.  In this case, the client is a C# server.

### Architecture
<br>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://assets.wristband.dev/docs/m2m-client-expressjs-demo-app/m2m-client-expressjs-demo-app-architecture-dark.png">
  <source media="(prefers-color-scheme: light)" srcset="https://assets.wristband.dev/docs/m2m-client-expressjs-demo-app/m2m-client-expressjs-demo-app-architecture-light.png">
  <img alt="entity model" src="https://assets.wristband.dev/docs/m2m-client-expressjs-demo-app/m2m-client-expressjs-demo-app-architecture-light.png">
</picture>

The demo server consists of two REST APIs: one that can be called without the need for an access token, and another that always requires a valid access token in the request headers.

## Acquiring an Access Token for the Server

Part of the server-startup process includes making a call to Wristband's `/token` endpoint to acquire an access token for this server using the Client Credentials grant type.  It will store the token, the access token expiration time, and a boolean flag that acts as a lock into local memory cache for later use.

You will interact with the server by calling the public data API.

## Demo Server Endpoints

### Public Data API

`GET http://localhost:6001/api/public/data`

This is the endpoint you can hit from any command line or API testing tool (cURL, Postman, etc.) without passing any access token.  When a request is sent to this API, the API will turn around and make an API call to the protected data API with the access token that was acquired during server startup.  This is to simulate something akin to a microservices environment where an upstream service would be responsible for sending an acess token with every downstream request. The expected response output of this API is:

`{ publicData: "hello world", protectedData: "here I am" }`

### Protected Data API

`GET http://localhost:6001/api/protected/data`

This endpoint is the downstream API called by the public data API, and it cannot be called without a valid access token.  When this API is invoked, a middleware is used to validate the following:
- The access token is present in the Authorization header (format = `Authorization: Bearer <access_token>`).
- The signature is valid when using the public keys as obtained from the Wristband JWKS endpoint.
- The access token is not expired.

## Getting New Access Tokens

There is an Axios request interceptor attached to the Protected API Client that is used for making requests to the protected data API from the public data API controller.  With each request made, that interceptor checks that an access token exists in the local memory cache and is also not expired.  If both conditions are met, it will stick the access token in the Authorization header automatically.  Otherwise, it won't proceed with the original downstream request until an attempt to get a new access token is complete.  Wristband's `/token` endpoint gets called with the Client Credentials grant type to get a new token, and the new token will be saved to local memory cache along with the new expiration time.

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions regarding this demo app.

<br/>
