The seeded administrator is `admin@gemora.com`. Its password is initially read
from `SeedUsers:AdminPassword` in secure configuration. Updating this setting
does not change the password of an existing account during normal startup.

To explicitly reset just this administrator to the configured password, run
from `Backend/Gemora.API`:

```powershell
dotnet run -- --reset-admin-password
```

Use the same environment and secure configuration as the API. For local user
secrets, use the Development environment. The command exits after updating the
administrator; it does not start the API or run migrations or demo seeding.
Other accounts retain their existing passwords. Sign in with the configured
`SeedUsers:AdminPassword` after the reset.
