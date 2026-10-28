# Setup (about 10 minutes)

## 1. Install tools

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — any 10.0.x. Check: `dotnet --list-sdks`
- [Visual Studio Code](https://code.visualstudio.com/)
- Git

## 2. Clone and set your git identity

```bash
git clone https://github.com/VeerM27/ANDtr.git
cd ANDtr
git config user.name "Your Name"
git config user.email "you@example.com"   # must be an email on your GitHub account
```

Use the same email as your GitHub account, or your commits won't be credited to you.

## 3. Open in VS Code

```bash
code .
```

Accept the prompt to install the **recommended extensions** (C# Dev Kit, GitLens, Error Lens, SQLite Viewer, and others). You can also find them under Extensions → *Recommended*.

## 4. Trust the HTTPS development certificate

```bash
dotnet dev-certs https --trust
```

## 5. Restore, build, test

```bash
dotnet tool restore
dotnet build
dotnet test
```

All three should finish with 0 errors, and all tests should pass.

## 6. Create your local test users

Choose your own passwords. They need at least 8 characters, with an upper-case letter and a digit. They're stored in user-secrets on your machine, not in the repo.

```bash
dotnet user-secrets --project src/ANDtr.Web set "Seed:Employee:Email" "employee@andtr.local"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Employee:Password" "<choose-one>"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Customer:Email" "customer@andtr.local"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Customer:Password" "<choose-one>"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Admin:Email" "admin@andtr.local"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Admin:Password" "<choose-one>"
```

## 7. Add the dataset

Put the client dataset in `data/private/` at the repo root. Git ignores this folder, so never move the dataset anywhere else in the repo.

The app expects `data/private/FDAdataset.xlsx`. If yours sits elsewhere, point at it instead:

```bash
dotnet user-secrets --project src/ANDtr.Web set "Seed:DatasetPath" "/absolute/path/to/FDAdataset.xlsx"
```

On the first run in Development the notices are imported from it into the empty `Notices` table — 616 rows. Later runs skip the import because the table is no longer empty. If the file is missing the console says so and the app still starts, just with no notices.

## 8. Run

```bash
dotnet run --project src/ANDtr.Web
```

Open <https://localhost:7095>. On first run the app creates `app.db`, the three roles, your three test users, one test customer organisation, and imports the dataset.

Log in as each user. Each one lands on its own page and sees only its own navigation: the employee on Search, the customer on Shared reviews, the admin on Users.

If the console shows `Skipping Employee test user`, `Skipping Customer test user` or `Skipping Admin test user`, step 6 is incomplete. Fix it and run the app again.

## 9. Workflow

Never push to `main`. For each piece of work:

```bash
git switch main && git pull
git switch -c feature/<short-name>
# ...commit small changes...
git push -u origin feature/<short-name>
```

Then open a pull request on GitHub. It needs one review and a green CI run before merging.

Full conventions are in [CONTRIBUTING.md](CONTRIBUTING.md).
