We need to refactor this project by Splitting the Web API project into two halves. 

First, a front end project using MVC that hosts the static HTML content and handles user actions with its own controllers and authentication.

Second, a backend project using Web API that hosts the current content-related APIs And requires a static bearer token in order to access those APIs.

The MVC site should create a generic proxy controller to pass the content related calls to the backend server. the mvc site should handle the providing the static bearer token to the proxy calls.
