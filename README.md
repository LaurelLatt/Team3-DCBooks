# Team3-DCBooks

Authors: Gabi Bekhrad, Jalen Myers, Laurel Latt

### The Program

DC Books is a library simulation application for DC comics. Users can create accounts, log in, browse the comic catalog, organize, sort, and filter books by attributes like year, issue number, and character, and check books in and out. 

---

## Start with Podman

1. **Make sure Podman is running**
  ```bash
   podman machine start
  ```
2. **Go to the project `src` folder**
  ```bash
   cd src
  ```
3. **Start the app + both databases**
  ```bash
   podman compose -f compose.yaml up --build
  ```
4. **Open the web UI (Swagger)**
  In your browser go to: `http://localhost:8080/swagger`
5. **Stop everything**
  Press `Ctrl+C` in the terminal, then run:

```bash
podman compose -f compose.yaml down
```

---

## Local Development (without Podman Compose)

When running the API locally with `dotnet run` instead of `podman compose`, you need two separate Postgres containers running on different ports — one for each database. This mirrors the two-container, two-service, two-database architecture used in the full Podman Compose setup.

**Architecture overview:**


| Container   | Database                                 | Local Port |
| ----------- | ---------------------------------------- | ---------- |
| `db-app`    | `project498_app` (users, checkouts)      | 5432       |
| `db-comics` | `project498_comics` (comics, characters) | 5433       |


`appsettings.Development.json` is already configured to point `AppConnection` at port 5432 and `ComicsConnection` at port 5433, so no config changes are needed.

**Steps:**

1. **Start the app database container**
  ```bash
   podman run -d --name db-app \
     -e POSTGRES_DB=project498_app \
     -e POSTGRES_USER=postgres \
     -e POSTGRES_PASSWORD=postgres \
     -p 5432:5432 \
     postgres:latest
  ```
2. **Start the comics database container**
  ```bash
   podman run -d --name db-comics \
     -e POSTGRES_DB=project498_comics \
     -e POSTGRES_USER=postgres \
     -e POSTGRES_PASSWORD=postgres \
     -p 5433:5432 \
     postgres:latest
  ```
3. **Run the API**
  ```bash
   cd src/Project498.WebApi
   dotnet run
  ```
   The API will create the database schema and seed both databases automatically on startup. Note: `dotnet run` serves the app at `**http://localhost:5031**` (not 8080 — that is the Podman Compose port). Open `http://localhost:5031/swagger` to access the UI.
4. **Stop and remove the containers when done**
  ```bash
   podman stop db-app db-comics
   podman rm db-app db-comics
  ```

> **Why two containers?** Each Postgres container runs its own independent database service. This matches the intended architecture — two containers, two services, two databases — rather than one Postgres server hosting both databases. Cross-database integrity (e.g. comics referenced in checkouts) is enforced at the application layer since SQL foreign keys cannot span separate database servers.

---

## Build and test

```bash
cd src
dotnet build Project498.sln
dotnet test Project498.sln
```

---

## Data integrity note

This project uses two databases:

- `project498_app` for users/checkouts
- `project498_comics` for comics/characters

Because `Checkouts.comicId` and `Comics.comicId` live in different databases, there is no cross-database SQL foreign key for comic ownership in checkouts. Integrity is enforced at the application layer in the API (e.g., checkout/return endpoints validate comic existence and status before writing checkout records).

---

## Local DB reset / reseed policy

Current local development uses ephemeral containers (no declared volumes), so rebuilding usually yields a clean database and reruns seeding.

Standard reset flow:

```bash
cd src
podman compose -f compose.yaml down
podman compose -f compose.yaml up --build
```

Default app DB seed user (created when app DB is empty):

- Username: `demo`
- Password: `Demo123`
- Email: `demo@demo.com`

If persistent volumes are introduced later, use:

```bash
cd src
podman compose -f compose.yaml down -v
podman compose -f compose.yaml up --build
```

