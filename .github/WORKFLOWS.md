# 🔄 GitHub Workflows Documentation

This project uses 3 GitHub Actions workflows for CI/CD:

## 📋 Workflows Overview

### 1. **CI - Build and Test** (`ci.yml`)
**Trigger**: Push to branches, Pull Requests, and **Tags**

**Purpose**: Validate that code compiles and all tests pass.

**Runs on**:
- ✅ Every push to `main`, `master`, `develop`
- ✅ Every Pull Request
- ✅ **Every tag `v*.*.*`** (important to validate before publish)

**Jobs**:
- Build on multiple OS (Ubuntu, Windows, macOS)
- Run all tests
- Code quality checks (formatting)

**Fails if**: Tests fail or code doesn't compile

---

### 2. **Release - Create GitHub Release** (`release.yml`)
**Trigger**: Tags `v*.*.*`

**Purpose**: Create a GitHub Release with automatic notes and artifacts.

**Runs when**:
- A tag like `v0.1.0`, `v1.0.0` is pushed

**Creates**:
- GitHub Release with automatic changelog
- Attaches NuGet package to release
- Generates release notes based on commits

**Requires**: CI to pass (implicit by GitHub Actions order)

---

### 3. **Publish - NuGet and GitHub Packages** (`publish.yml`)
**Trigger**: Tags `v*.*.*` (after CI)

**Purpose**: Publish package to NuGet.org and GitHub Packages.

**Publishes to**:
- ✅ **NuGet.org**: https://www.nuget.org/packages/Diiwo.Identity
- ✅ **GitHub Packages**: https://github.com/Diiwo/diiwo-identity/packages

**Requires**:
- `NUGET_API_KEY` secret configured in repository
- CI to pass (workflow runs after)

---

## 🚀 Complete Release Flow

When you push a tag, this is the automatic flow:

```
git tag v0.1.0
git push origin v0.1.0
    ↓
┌─────────────────────────────┐
│ 1. CI Workflow              │
│    - Build on 3 OS          │
│    - Run all tests ✅       │
│    - Code quality checks    │
└─────────────────────────────┘
    ↓ (if passes)
┌─────────────────────────────┐
│ 2. Release Workflow         │
│    - Create GitHub Release  │
│    - Generate changelog     │
│    - Attach .nupkg          │
└─────────────────────────────┘
    ↓ (in parallel)
┌─────────────────────────────┐
│ 3. Publish Workflow         │
│    - Pack NuGet package     │
│    - Publish to NuGet.org   │
│    - Publish to GitHub Pkg  │
└─────────────────────────────┘
```

**Estimated total time**: 5-10 minutes

---

## 🔧 Required Configuration

### Required Secrets

Configure these secrets in: `Settings` → `Secrets and variables` → `Actions`

1. **`NUGET_API_KEY`** (Required)
   - Get it from: https://www.nuget.org/account/apikeys
   - Permissions: `Push new packages and package versions`

### Repository Permissions

In `Settings` → `Actions` → `General` → `Workflow permissions`:
- ✅ Enable: "Read and write permissions"
- ✅ Enable: "Allow GitHub Actions to create and approve pull requests"

---

## 📝 How to Create a Release

### Option 1: Automatic (Recommended)

```bash
# 1. Make sure you're on main/master
git checkout main
git pull

# 2. Update version in .csproj if needed
# (Optional, workflow uses tag version)

# 3. Create and push the tag
git tag v0.1.0
git push origin v0.1.0

# 4. Done! Workflows run automatically
```

### Option 2: Manual (from GitHub)

1. Go to `Actions` on GitHub
2. Select the workflow you want to run
3. Click "Run workflow"
4. Enter version manually

---

## 🧪 Local Testing Before Release

Before creating a tag, always verify locally:

```bash
# Restore and build
dotnet restore src/Diiwo.Identity.csproj
dotnet build src/Diiwo.Identity.csproj --configuration Release

# Run tests
dotnet test --configuration Release

# Pack (optional, to verify)
dotnet pack src/Diiwo.Identity.csproj --configuration Release --output ./test-artifacts
```

---

## 🐛 Troubleshooting

### "CI fails on tag push"
- **Cause**: Tests fail or code doesn't compile
- **Solution**: Review CI workflow logs, fix tests, delete tag and recreate

```bash
# Delete local and remote tag
git tag -d v0.1.0
git push origin :refs/tags/v0.1.0

# Fix the issue, then recreate tag
git tag v0.1.0
git push origin v0.1.0
```

### "Publish fails with 401 Unauthorized"
- **Cause**: `NUGET_API_KEY` not configured or expired
- **Solution**: Regenerate API key on NuGet.org and update secret

### "Package already exists"
- **Cause**: Trying to publish a version that already exists
- **Solution**: Workflow uses `--skip-duplicate`, so this shouldn't fail. If it does, increment version.

---

## 📊 Versioning

We use **Semantic Versioning** (SemVer):

- **v1.0.0**: Major release (breaking changes)
- **v0.2.0**: Minor release (new features, backward compatible)
- **v0.1.1**: Patch (bug fixes)
- **v0.1.0-beta**: Pre-release (testing)

---

## 🔗 Useful Links

- **NuGet Package**: https://www.nuget.org/packages/Diiwo.Identity
- **GitHub Packages**: https://github.com/Diiwo/diiwo-identity/packages
- **Diiwo.Core Dependency**: https://www.nuget.org/packages/Diiwo.Core
- **GitHub Actions Docs**: https://docs.github.com/en/actions

---

## 📦 After Publishing

Once published, users can install with:

```bash
# From NuGet.org (public)
dotnet add package Diiwo.Identity --version 0.1.0

# From GitHub Packages (requires authentication)
dotnet nuget add source https://nuget.pkg.github.com/Diiwo/index.json -n github
dotnet add package Diiwo.Identity --version 0.1.0
```

---

**Questions?** Open an issue in the repository.
