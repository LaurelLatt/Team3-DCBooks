## Team3-DCBooks

Authors: Gabi Bekhrad, Jalen Myers, Laurel Latt

### The Program
DC Books is a library simulation application for DC comics. Users can create accounts, log in, browse the comic catalog, organize, sort, and filter books by attributes like year, issue number, and character, and check books in and out. 

---

## Quick start with Podman (recommended)

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