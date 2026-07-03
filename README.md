# ERP Cosmétique — Application complète

Système de gestion pour marchands de produits cosmétiques : backend ASP.NET Core 8 + frontend React.

## Démarrage rapide

### Option A — Développement (2 terminaux)

**Terminal 1 — API :**
```bash
cd ErpCosmetiqueApi
dotnet run
```
API : http://localhost:5138

**Terminal 2 — Interface :**
```bash
cd ErpCosmetiqueApi/frontend
npm install
npm run dev
```
Interface : http://localhost:5173

### Option B — Production (tout-en-un)

```bash
cd ErpCosmetiqueApi/frontend
npm install
npm run build
xcopy /E /I /Y dist ..\wwwroot

cd ..
dotnet run
```
Ouvrir : http://localhost:5138

## Connexion

| Utilisateur | Mot de passe | Rôle |
|-------------|--------------|------|
| admin | admin123 | Administrateur |
| vendeur | vendeur123 | Vendeur |

## Modules

- Tableau de bord (ventes, dépenses, bénéfice, caisse, ruptures)
- Caisse / vente au comptoir (POS)
- Produits, catégories, marques, stock
- Clients, fidélité, dettes
- Fournisseurs et achats
- Dépenses caisse (repas, transport…)
- Personnel, présences, avances
- Factures PDF automatiques
- Rapports journalier / hebdo / mensuel / annuel

## API Swagger

http://localhost:5138/swagger (avec token JWT dans Authorize)

## Stack

- **Backend** : ASP.NET Core 8, EF Core, SQLite, JWT, QuestPDF
- **Frontend** : React 18, TypeScript, Vite, React Router
