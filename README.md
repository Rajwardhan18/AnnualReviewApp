# ARISe — Annual Plan & Review

**ARISe** (Achieve · Reflect · Innovate · Strategize — *for excellence*) is a web
application for running the annual plan-and-review cycle for developers, by **Sparrow**.
**.NET 9** Web API backend + **React (Vite + TypeScript)** frontend, with **SQLite**
storage and **JWT** authentication. The UI uses the ARISe theme — an emerald/gold palette
with the four pillars as accent colours and a sunrise brand mark.

---

## Quick start

Two terminals from the `AnnualReviewApp` folder:

```bash
./run-backend.sh     # API  → http://localhost:5099  (Swagger at /swagger)
```

```bash
./run-frontend.sh    # App  → http://localhost:5173
```

Then open **http://localhost:5173**. The frontend proxies `/api/*` to the backend,
so no CORS setup is needed in dev. The SQLite database and all master data are
created and seeded automatically on first run.

> The scripts point at the Homebrew `dotnet@9` SDK and Node automatically.
> If you run the projects manually, ensure `dotnet` (9.x) and `node` are on your PATH.

### Seeded / demo accounts

| Role      | Email                | Password    |
|-----------|----------------------|-------------|
| Admin     | `admin@company.com`  | `Admin@123` |
| Developer | `aisha@company.com`  | `Passw0rd!` |
| Developer | `gopal@company.com`  | `Passw0rd!` |
| Manager   | `manny@company.com`  | `Passw0rd!` |
| Manager   | `morgan@company.com` | `Passw0rd!` |

The seeded demo dataset includes 7 developers with fully-completed reviews (all sign in with
`Passw0rd!`: aisha, ben, chloe, diego, ema, farah, gopal), 2 managers, and a shared peer
reviewer (`quinn@company.com`) — enough to populate the ratings dashboard and normal curve.
Delete `backend/PlanReview.Api/planreview.db` to start clean; master data and the admin
account re-seed automatically (developers/reviews do not).

---

## Architecture

```
AnnualReviewApp/
├─ backend/PlanReview.Api/       # .NET 9 Web API
│  ├─ Models/                    # EF Core entities + enums
│  ├─ Data/                      # DbContext + seeder
│  ├─ DTOs/                      # request/response contracts
│  ├─ Services/                  # JWT token service, review rules
│  ├─ Controllers/               # Auth, MasterData, Users, Cycles, Reviews
│  └─ Migrations/                # EF Core migration (InitialCreate)
└─ frontend/                     # React + Vite + TypeScript
   └─ src/
      ├─ auth/                   # AuthContext (JWT in localStorage)
      ├─ api/                    # fetch wrapper
      ├─ components/             # Layout (collapsible sidebar), StarRating
      └─ pages/                  # Login, Register, Dashboard, Users, ReviewEditor, ReviewView, Admin
```

### Data model

- **User** — `Developer | Manager | Admin`. Developers carry a `Function` and a `Role`.
- **Function** — discipline (Frontend / Backend Developer). Owns **Roles**.
- **Role** — career level within a function (SDE-1, SDE-2, …). Mapped to skills via **RoleSkill**.
- **Skill** — master list; **RoleSkill** maps skills to a role.
- **CompanyTrait** — Leadership, Ownership, Integrity, … Every goal is tagged to one.
- **ReviewCycle** — annual cycle; releasing it creates a **Review** per developer.
- **Review** — a developer's plan for a cycle: **Goals** (SMART), **SkillRatings** (self),
  a developer-selected peer, admin-assigned **ReviewReviewers** (2 managers + 1 peer),
  and **ReviewerAssessments** (rating + feedback + per-skill ratings).

---

## How each requirement is implemented

| # | Requirement | Where |
|---|-------------|-------|
| 1 | Register Developers & Managers | `AuthController.Register`, `RegisterPage` (Admin can't self-register) |
| 2 | Developer function = Frontend / Backend | `Function` entity, seeded; chosen at registration |
| 3 | Roles per function (SDE-1, SDE-2, …) | `Role` entity under `Function`; Admin console → Functions & Roles |
| 4 | Skills master mapped per role | `Skill` + `RoleSkill`; Admin console → Skills, Role → Skills |
| 5 | Cycle released annually per developer | `CyclesController.Release` creates a `Review` for every developer |
| 6 | SMART goal inputs | `Goal` (Specific/Measurable/Achievable/Relevant/TimeBound); `ReviewEditorPage` |
| 7 | Sections: ≥5 professional goals, skill rating per role skill, ≥2 personal goals | `ReviewRules` + `ReviewsController.Submit` validation; editor UI |
| 8 | Each goal tagged to a company trait | `Goal.CompanyTraitId`; trait dropdown per goal |
| 9 | Submitted at the start of the cycle | `Review.Status` (Draft → Submitted), `SubmittedAt`; locks on submit |
| 10 | Developer selects a peer | `Review.SelectedPeerId`; peer picker in editor |
| 11 | Admin assigns 2 managers + 1 peer for review & rating | `ReviewsController.Assign`; `ReviewerAssessment` for their ratings |

### Admin & UI features

- **Admin user management** — the **Users** page (admin) lists every registered user with
  type/function/role and creates Developers (with function + role), Managers, and Admins
  directly (`POST /api/users`). Self-registration remains available for Developers/Managers.
  Admins can **activate/deactivate** any user (`PUT /api/users/{id}/active`) — deactivated users
  cannot sign in and are excluded from peer/manager pickers and cycle releases (you cannot
  deactivate your own account).
- **Interface** — a collapsible sidebar with **minimalist flat line icons**, and content laid out
  **full-width** to use the whole space beside the sidebar.
- **Collapsible sidebar** — the left nav collapses to an icon rail (state persisted); it
  auto-collapses on narrow screens.
- **Tabbed review** — the plan editor and review view present **Professional Goals**,
  **Skill Assessment**, **Personal Goals**, and **Key Achievements** as tabs; SMART fields
  are laid out one below the other, and each goal is a collapsible **accordion** showing a
  Complete/Incomplete status and its progress badge.
- **Draft vs submit** — **Save draft** persists partial work at any time (empty SMART fields,
  no trait yet). The "minimum filling criteria" checklist sits **at the top** of the editor with
  the Save/Submit actions; Submit is enabled (and server-enforced) only once ≥5 professional +
  ≥2 personal goals are fully filled, all role skills are rated, and a peer is selected. New
  reviews open with a single starter goal per section — the developer adds the rest.
- **Goal progress tracking** — each professional/personal goal carries a **status**
  (Not Started / In Progress / Completed / Dropped), a **completion %**, a comment and a date.
  Progress is editable through the year even after the plan is locked (`PUT /reviews/{id}/progress`).
- **Skill-assessment extras** — the Skill Assessment tab also captures **Initiatives Undertaken**
  (research contributions) and **Future Skills to Acquire**.
- **Previous Year Achievements** — a tab of projects delivered last year (**minimum 5**), each
  with project name, client, work description and an optional trait. The developer cannot rate
  them — each of the **two assigned managers rates every achievement** (Manager 1 and Manager 2
  slots), and the ratings show back read-only to the developer.
- **Personal goals are simple** — a title, a **target**, and progress (status / completion %);
  the SMART template applies only to professional goals.
- **Reviewer anonymity** — the developer never sees who their reviewers are: reviewer and
  assessment authorship are masked server-side to "Manager 1 / Manager 2 / Peer".
- **Self-selected peer becomes a reviewer** — on submit, the developer's chosen peer is
  automatically added as a Peer reviewer, so their peer review is captured and visible to the
  admin without waiting for a separate admin assignment.
- **Half-yearly checkpoint** — the admin can release a mid-year review on a cycle; developers
  update their goal progress and add a **mid-year reflection**, then **submit** it — after which
  it **locks** (`POST /reviews/{id}/submit-midyear`). Manager and peer assessments stay at year-end.
- **Submit & freeze** — once a reviewer submits their assessment it is **locked** (no re-submit),
  and the dashboards reflect submitted state ("Submitted" / "Mid-year submitted") instead of the
  action button.
- **Developer performance dashboard** — a **My Performance** page shows each developer their
  self-progress (goal completion, status breakdown) always, and — once the admin releases
  ratings — their self / peer / manager scores, **overall average**, weighted final, percentile
  and performance **band** vs the team average (reviewer names hidden). `GET /api/performance/me`.
- **Release ratings & end cycle (separate)** — the admin can **release ratings**
  (`POST /cycles/{id}/release-ratings`) to reveal them on My Performance, and separately
  **end the cycle** (`POST /cycles/{id}/end`). Ending is only allowed once the half-yearly review
  has been submitted by everyone and all manager & peer reviews are submitted.
- **Notifications** — every developer is notified when the annual plan (with target dates and
  reminders) or the half-yearly checkpoint is released, and assigned managers/peers are notified
  on assignment. Notifications appear in-app (a bell with an unread badge) and are **also emailed
  when SMTP is configured**. Email is opt-in via `appsettings.json` → `Email.Enabled` (default
  `false`, so nothing is emailed until you add SMTP credentials); everything is still recorded
  in-app in the meantime.
- **Reviewer assignment (any time)** — from any review (admin → dashboard → *Open*), the admin
  assigns exactly 2 managers and 1 peer — even before the developer submits. Reviewers can only
  submit their assessment once the developer has submitted the plan.
- **Ratings on a 1–10 scale** — self, peer, and manager skill/overall ratings use a 10-point
  star scale with a numeric readout; the SMART time-bound field is a target-**date** picker.
- **Weighted normalized rating** — each developer's final is a weighted average of the
  component scores: **Self 10% · Peer 20% · Manager 1 30% · Manager 2 40%** (the two managers
  are distinguished by the order the admin assigns them, stored as a per-reviewer weight).
  The self score is the average of the developer's skill self-ratings; each reviewer contributes
  their overall rating. Missing components re-normalize across whatever is present.
- **Normal-curve fit** — the cohort of finals within a cycle is fitted to a normal curve:
  mean, standard deviation, each developer's **z-score**, **percentile** (Φ(z)), a **curved**
  1–10 score, and a **performance band** (Needs Improvement / Below / Meets / Exceeds /
  Outstanding, split at ±½σ and ±1½σ).
- **Ratings dashboard** — an admin **Ratings** page lists every developer with their self /
  peer / manager-1 / manager-2 scores and weighted final, plus a bell-curve chart plotting
  each developer and shading the performance bands.

### Review lifecycle

```
Draft ──submit──▶ Submitted ──admin assign──▶ InReview ──all reviewers submit──▶ Completed
```

Submission is validated server-side: at least 5 professional goals, at least 2 personal
goals, every role skill rated, all SMART fields present, and a peer selected.

---

## API surface (selected)

| Method & path | Auth | Purpose |
|---|---|---|
| `POST /api/auth/register` \| `login` \| `GET /me` | — / JWT | Accounts |
| `GET /api/functions` \| `roles` | anon | Registration pickers |
| `GET/POST /api/skills`, `PUT /api/roles/skills` | Admin | Skills & role mapping |
| `GET/POST /api/traits` | JWT / Admin | Company traits |
| `GET/POST /api/cycles`, `POST /api/cycles/{id}/release` | Admin | Cycles |
| `GET /api/users` \| `POST /api/users` | Admin | List all users · create Developer/Manager/Admin |
| `POST /api/users/{id}/reset-password` | Admin | Reset any user's password to a new value |
| `GET /api/dashboard/ratings?cycleId=` | Admin | Weighted finals + normal-curve fit per developer |
| `GET /api/reviews/mine` \| `assigned` \| `(all)` | JWT | Review lists |
| `PUT /api/reviews/{id}/plan`, `POST …/submit` | Developer | Fill & submit plan |
| `POST /api/reviews/{id}/assign` | Admin | Assign 2 managers + peer |
| `POST /api/reviews/{id}/assessment` | Reviewer | Submit rating & feedback |
| `POST /api/cycles/{id}/release-halfyearly` | Admin | Open the mid-year checkpoint + notify developers |
| `GET /api/notifications/mine` \| `POST …/{id}/read` \| `read-all` | JWT | In-app notifications |

Explore everything interactively at **http://localhost:5099/swagger**.

---

## Notes & production hardening

- The JWT signing key in `appsettings.json` is a **development placeholder** — move it to a
  secret store and rotate it for any real deployment.
- SQLite is used for zero-setup local runs. To switch to SQL Server, change the
  `UseSqlite(...)` call in `Program.cs` and the connection string; the EF model is unchanged.
- Passwords are hashed with BCrypt. Enums are serialized as strings across the API.
