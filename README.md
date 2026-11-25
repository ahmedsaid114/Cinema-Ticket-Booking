# TaskCinema - Cinema ticket booking

Cinema MVC application using ASP.NET Core Identity, role-based access, and SQL Server.

## Features

- Browse movies, actors and categories
- Ticket checkout with Stripe (test mode), charged in EGP
- Customer accounts with ASP.NET Core Identity and email confirmation
- Role-based areas: Customer, Admin and SuperAdmin dashboards
- Admin management of movies and categories
- Generic repository and Unit of Work over SQL Server

## Run

For a guided Arabic first-time setup of the SuperAdmin/Admin accounts, database, and Stripe test mode, see [SETUP_AR.md](SETUP_AR.md).

1. Install the .NET 10 SDK.
2. Set `ConnectionStrings:CinemaProject` in `appsettings.json` to your SQL Server. The current example uses the default local instance (`Data Source=.`) and integrated Windows authentication.
3. Open `TaskCinema.csproj` in Visual Studio and run with the HTTPS profile, or use a terminal in this folder:

   ```powershell
   dotnet restore
   dotnet run
   ```

In Development, the app applies the EF Core migrations at startup. The database server must be running and the account in the connection string must be able to create/update the `CinemaProject` database.

## Customer account pages

- `/Customer/Account/Register`
- `/Customer/Account/Login`
- `/Customer/Account/ForgotPassword`

ASP.NET Core Identity uses the existing `CustomerAccounts` table for users and adds its role, claims, login, and token tables. The migration keeps existing customer rows and password hashes, gives unassigned legacy accounts the Customer role, and preserves ticket order links. Password reset uses Identity's token providers and sends email through `Email:Smtp`.

Set email credentials with User Secrets from this folder:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Email:Smtp:Host" "smtp.your-provider.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:EnableSsl" "true"
dotnet user-secrets set "Email:Smtp:UserName" "your-smtp-user"
dotnet user-secrets set "Email:Smtp:Password" "your-smtp-password"
dotnet user-secrets set "Email:Smtp:FromAddress" "your-sender@example.com"
dotnet user-secrets set "Email:Smtp:FromName" "CineVerse"
```

## Ticket checkout and Stripe

Movie prices are treated as single ticket prices and charged in EGP. Customers choose 1–10 tickets from a movie details page. Stripe-hosted Checkout handles card entry; the app records an order and only counts it as paid after Stripe confirms payment through the webhook (or the verified Checkout return).

The app presents ticket charges in EGP. Presentment and settlement options depend on the Stripe account's country and payout setup; confirm them in Stripe Dashboard before accepting live payments. Stripe requires the business to be based in a supported country/region.

Set the API secret, webhook signing secret, and admin login with User Secrets. Do not commit or paste these values into chat:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "use-a-unique-long-password"
```

For local testing, run Stripe CLI and forward its test webhook to `https://localhost:<port>/payments/stripe/webhook`. Subscribe to `checkout.session.completed`, `checkout.session.expired`, `checkout.session.async_payment_succeeded`, and `checkout.session.async_payment_failed`. Use test API keys and Stripe test cards until the account, webhook endpoint, and site are ready for live mode. In production, configure `Stripe:SecretKey` and `Stripe:WebhookSecret` through the hosting provider's secret store and register the public HTTPS webhook URL in Stripe Dashboard.

## Roles and dashboards

The app assigns one active role to each account:

- **Customer**: `/Customer/Dashboard` shows the account's ticket orders. Customers can browse movies and check out.
- **Admin**: `/Admin/Home` shows sales and order activity. Admins can manage movies and categories.
- **SuperAdmin**: `/Admin/SuperAdmin` shows role totals and opens user and role management. Super Admins can also use the Admin dashboard and catalog tools.

The public navigation shows a role-specific dashboard button after sign in. Server-side `[Authorize]` checks enforce each role; hiding a link is not the only protection. Only a SuperAdmin can change account roles, and the app prevents demoting the last SuperAdmin or the currently signed-in SuperAdmin.

Set distinct bootstrap accounts with User Secrets. The app seeds the three roles on startup, creates the configured accounts if they do not already exist, and assigns their roles:

```powershell
dotnet user-secrets set "SuperAdmin:Email" "owner@example.com"
dotnet user-secrets set "SuperAdmin:Password" "use-a-unique-long-password"
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "use-a-different-long-password"
```

Do not use the same email for the SuperAdmin and Admin bootstrap accounts. Existing users that do not yet have a role receive Customer access. Existing deployments must apply pending migrations before starting the updated app in production. Development applies migrations automatically. The dashboard summarizes orders from the app database; the Stripe Dashboard remains the source of truth for the Stripe balance and payouts.

## Repository structure

`Repositories/` contains the generic repository, repository interface, and Unit of Work. Customer movie, admin movie, category, and customer account services use the Unit of Work to access SQL Server.
