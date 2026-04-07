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

3. **Start the app + database**

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
