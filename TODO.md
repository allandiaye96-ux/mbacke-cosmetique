# ERP Cosmétique — Application complète ✅

## Statut : fonctionnel

Backend ASP.NET Core 8 + Frontend React + Auth JWT + Factures PDF

## Démarrage

### Mode production (recommandé — tout en un)

```bash
cd ErpCosmetiqueApi
dotnet run
```

Ouvrir : **http://localhost:5138**

### Mode développement (hot reload frontend)

Terminal 1 :
```bash
cd ErpCosmetiqueApi
dotnet run
```

Terminal 2 :
```bash
cd ErpCosmetiqueApi/frontend
npm run dev
```

Ouvrir : **http://localhost:5173**

## Connexion

| Utilisateur | Mot de passe | Rôle |
|-------------|--------------|------|
| **admin** | admin123 | Administrateur |
| **vendeur** | vendeur123 | Vendeur |

## Modules disponibles dans l'interface

1. **Tableau de bord** — ventes, dépenses, bénéfice, caisse, ruptures, dettes
2. **Caisse / Vente** — point de vente (POS) avec remises et modes de paiement
3. **Historique ventes** — consultation et annulation
4. **Produits** — catalogue, catégories, marques, stock
5. **Clients** — fidélité, dettes, paiements
6. **Fournisseurs** — gestion et soldes
7. **Achats** — réception marchandises + stock
8. **Caisse & Dépenses** — repas, transport, carburant, etc.
9. **Personnel** — employés, présences, avances salaire
10. **Factures** — historique + téléchargement PDF
11. **Rapports** — journalier, hebdo, mensuel, annuel

## Rebuild frontend

```bash
cd frontend
npm run build
```

Les fichiers sont générés dans `wwwroot/`.

## API Swagger

http://localhost:5138/swagger

## Stack technique

- Backend : ASP.NET Core 8, EF Core, SQLite, JWT, QuestPDF, BCrypt
- Frontend : React 18, TypeScript, Vite, React Router
