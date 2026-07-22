# Deploying Prima Estates to Railway

This guide takes you from the project folder to a live site at
`realestate.ardwebdesign.com`, with PostgreSQL and Cloudinary image hosting.

The app is written to run **both** locally (SQLite, no setup) and in production
(PostgreSQL on Railway) with no code changes — it picks the database automatically
based on the environment variables present.

---

## 0. What you need first

- A **Railway** account
- A **GitHub** account + `git` installed locally
- A **Cloudinary** account (free tier is fine) — grab your `CLOUDINARY_URL` from
  the Cloudinary dashboard (Account Details → "API Environment variable", it looks
  like `cloudinary://123456789:abcdef@your-cloud-name`)

---

## 1. Push the project to GitHub

From the project folder:

```bash
git init
git add .
git commit -m "Prima Estates - initial"
```

Create an empty repo on GitHub (no README), then:

```bash
git remote add origin https://github.com/<you>/prima-estates.git
git branch -M main
git push -u origin main
```

The included `.gitignore` keeps `bin/`, `obj/`, the local `.db` file, and uploads
out of the repo. Secrets live only in Railway, never in the code.

---

## 2. Create the Railway project

1. Railway dashboard → **New Project** → **Deploy from GitHub repo**
2. Pick your `prima-estates` repo
3. Railway detects the **Dockerfile** and starts building automatically

The first build takes a few minutes (ASP.NET images are chunky). Let it finish —
it will fail to start fully until the database exists, which is the next step.

---

## 3. Add PostgreSQL

1. In your project → **New** → **Database** → **Add PostgreSQL**
2. Railway provisions it and automatically exposes a `DATABASE_URL` variable to
   your app service. **You don't need to copy it manually** — the app reads it.

> If your app service doesn't automatically see `DATABASE_URL`, go to the app
> service → **Variables** → **Add Reference** → select the Postgres `DATABASE_URL`.

---

## 4. Set the environment variables

On the **app service** → **Variables** tab, add:

| Variable | Value |
|---|---|
| `ADMIN_USERNAME` | your chosen admin login (e.g. `sezhian`) |
| `ADMIN_PASSWORD` | a strong password (this seeds the first admin account) |
| `CLOUDINARY_URL` | your `cloudinary://...` string |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

`DATABASE_URL` is already there from step 3. Save — Railway redeploys.

On startup the app creates all tables in Postgres and seeds the sample data +
your admin account. Watch the **Deploy logs**; you want to see it reach
"Application started".

---

## 5. Test on the Railway URL

1. App service → **Settings** → **Networking** → **Generate Domain**
2. Open the `xxxx.up.railway.app` URL
3. Check: homepage loads, listings show, `/account/login` works with your new
   `ADMIN_USERNAME` / `ADMIN_PASSWORD`, and uploading an image in the admin
   saves it (it should return a `res.cloudinary.com` URL)

Fix anything here **before** attaching your domain.

---

## 6. Point realestate.ardwebdesign.com at Railway

1. Railway → app service → **Settings** → **Networking** → **Custom Domain**
2. Enter `realestate.ardwebdesign.com` → Railway shows a **CNAME target**
   (something like `xxxx.up.railway.app`)
3. In **Cloudflare** (ardwebdesign.com DNS):
   - Add a **CNAME** record
   - Name: `realestate`
   - Target: the Railway CNAME target
   - Proxy status: **DNS only (grey cloud)** — needed so Railway can issue SSL
4. Wait a few minutes. Railway auto-issues the TLS certificate.
5. Visit `https://realestate.ardwebdesign.com` — done.

Once it's live and green, you *may* switch Cloudflare back to proxied (orange
cloud) if you want CDN/caching, but grey-cloud is the safe default.

---

## After go-live

- **Change nothing in code to update content** — use the admin dashboard.
- **To deploy code changes:** `git push` — Railway rebuilds automatically.
- **Uploads** now live in Cloudinary (folder `prima-estates`), so they survive
  redeploys. The old local `wwwroot/uploads` path is only used when no
  `CLOUDINARY_URL` is set (i.e. local dev).

---

## Optional: switch to versioned EF migrations later

The app currently uses `EnsureCreated()` — perfect for launch, but it can't alter
an existing table when you change the model later. When you're ready to evolve the
schema in production without wiping data, switch to migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
```

Then in `Program.cs` replace `db.Database.EnsureCreated();` with
`db.Database.Migrate();`, commit, and push. From then on each schema change is a
new `dotnet ef migrations add <Name>` + push.

> Do this on a fresh database, or take a backup first — you can't retrofit
> migrations onto an `EnsureCreated` database without a little care.

---

## Local development still works unchanged

Nothing above affects local dev. On your PC, with no `DATABASE_URL` or
`CLOUDINARY_URL` set, `dotnet run` uses SQLite and local-disk uploads exactly as
before. The default admin stays `admin` / `Admin@123` locally.
