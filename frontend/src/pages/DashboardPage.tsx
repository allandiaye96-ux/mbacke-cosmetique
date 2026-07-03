import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { dashboardApi, productsApi } from '../api/client'
import { formatMoney } from '../api/types'
import type { Dashboard, Product } from '../api/types'
import { PageHeader, Card, StatCard, Table, Alert } from '../components/Layout'

export default function DashboardPage() {
  const [data, setData] = useState<Dashboard | null>(null)
  const [lowStock, setLowStock] = useState<Product[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([dashboardApi.get(), productsApi.lowStock()])
      .then(([d, ls]) => { setData(d); setLowStock(ls) })
      .catch((e) => setError(e.message))
  }, [])

  if (error) return <Alert type="error">{error}</Alert>
  if (!data) return <div className="loading">Chargement...</div>

  return (
    <>
      <PageHeader title="Tableau de bord" subtitle="Vue d'ensemble de votre boutique aujourd'hui" />
      <div className="stats-grid">
        <StatCard label="Ventes du jour" value={formatMoney(data.todaySales)} tone="sales" />
        <StatCard label="Dépenses du jour" value={formatMoney(data.todayExpenses)} tone="expense" />
        <StatCard label="Bénéfice du jour" value={formatMoney(data.todayProfit)} tone="profit" />
        <StatCard label="Montant en caisse" value={formatMoney(data.cashBalance)} tone="cash" />
        <StatCard label="Dettes clients" value={formatMoney(data.customerDebts)} tone="debt" />
        <StatCard label="Ventes (nombre)" value={String(data.todaySaleCount)} tone="default" />
      </div>

      <div className="grid-2">
        <Card>
          <h3>Actions rapides</h3>
          <div className="quick-actions">
            <Link to="/pos" className="quick-btn">🛒 Nouvelle vente</Link>
            <Link to="/cash" className="quick-btn">💸 Enregistrer dépense</Link>
            <Link to="/products" className="quick-btn">💄 Gérer produits</Link>
            <Link to="/reports" className="quick-btn">📈 Voir rapports</Link>
          </div>
        </Card>

        <Card>
          <h3>Produits en rupture ({lowStock.length})</h3>
          {lowStock.length === 0 ? (
            <p className="muted">Tous les stocks sont OK</p>
          ) : (
            <Table headers={['Produit', 'Stock', 'Minimum']}>
              {lowStock.slice(0, 8).map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td>
                  <td className="text-danger">{p.stockQuantity}</td>
                  <td>{p.minStockLevel}</td>
                </tr>
              ))}
            </Table>
          )}
        </Card>
      </div>
    </>
  )
}
