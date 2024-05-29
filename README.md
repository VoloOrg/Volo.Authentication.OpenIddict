# Volo.Authentication.OpenIddict
# Volo.Authentication.OpenIddict.API

These projects are not stand-alone libraries and they meant to be copy-paste and adjusted during development.
They need their front-end projects (Angular or React), which are also developed.
This project uses cookie (HTTP only, same site strict, secure), so all of the calls should go through the API project.
API project uses middlewares to direct the calls to the IdP server.
API project has some user management functionality implementation just to cover the main flow, but it can be extended.
There are comments in code to describe or emphasize the importance of the added configurations/options/codes.

Used main technologies : 
- OpenIddict
- Microsoft.AspNetCore.Identity
- Microsoft.EntityFrameworkCore
- Sendgrid (mailing service)