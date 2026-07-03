import { useEffect, useState } from 'react'
import { reportsApi } from '../api/client'
import { formatMoney } from '../api/types'
import type { DailyReport, PeriodReport } from '../api/types'
import { PageHeader, Card, Table, StatCard, Alert } from '../components/Layout'

export default function ReportsPage() {
  const [tab, setTab] = useState<'daily' | 'weekly' | 'monthly' | 'yearly'>('daily')
  const [daily, setDaily] = useState<DailyReport | null>(null)
  const [period, setPeriod] = useState<PeriodReport | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    setError('')
    if (tab === 'daily') {
      reportsApi.daily().then(setDaily).catch((e) => setError(e.message))
    } else {
      const fn = tab === 'weekly' ? reportsApi.weekly : tab === 'monthly' ? reportsApi.monthly : reportsApi.yearly
      fn().then(setPeriod).catch((e) => setError(e.message))
    }
  }, [tab])

  return (
    <>
      <PageHeader title="Rapports" subtitle="Journalier, hebdomadaire, mensuel et annuel" />
      {error && <Alert type="error">{error}</Alert>}
      <div className="tabs">
        {(['daily', 'weekly', 'monthly', 'yearly'] as const).map((t) => (
          <button key={t} className={tab === t ? 'tab active' : 'tab'} onClick={() => setTab(t)}>
            {t === 'daily' ? 'Journalier' : t === 'weekly' ? 'Hebdomadaire' : t === 'monthly' ? 'Mensuel' : 'Annuel'}
          </button>
        ))}
      </div>

      {tab === 'daily' && daily && (
        <>
          <div className="stats-grid">
            <StatCard label="Ventes" value={formatMoney(daily.totalSales)} tone="sales" />
            <StatCard label="Dépenses" value={formatMoney(daily.totalExpenses)} tone="expense" />
            <StatCard label="Bénéfice" value={formatMoney(daily.profit)} tone="profit" />
            <StatCard label="Nb ventes" value={String(daily.saleCount)} tone="default" />
          </div>
          <div className="grid-2">
            <Card>
              <h3>Ventes du jour</h3>
              <Table headers={['#', 'Client', 'Total', 'Articles']}>
                {daily.sales.map((s) => (
                  <tr key={s.id}><td>{s.id}</td><td>{s.customerName}</td><td>{formatMoney(s.totalAmount)}</td><td>{s.itemCount}</td></tr>
                ))}
              </Table>
            </Card>
            <Card>
              <h3>Dépenses du jour</h3>
              <Table headers={['Motif', 'Montant']}>
                {daily.expenses.map((e) => (
                  <tr key={e.id}><td>{e.motive}</td><td>{formatMoney(e.amount)}</td></tr>
                ))}
              </Table>
            </Card>
          </div>
        </>
      )}

      {tab !== 'daily' && period && (
        <>
          <div className="stats-grid">
            <StatCard label="Ventes" value={formatMoney(period.totalSales)} tone="sales" />
            <StatCard label="Achats" value={formatMoney(period.totalPurchases)} tone="expense" />
            <StatCard label="Dépenses" value={formatMoney(period.totalExpenses)} tone="expense" />
            <StatCard label="Bénéfice" value={formatMoney(period.profit)} tone="profit" />
          </div>
          <div className="grid-2">
            <Card>
              <h3>Produits les plus vendus</h3>
              <Table headers={['Produit', 'Qté', 'CA']}>
                {period.topProducts.map((p) => (
                  <tr key={p.productId}><td>{p.productName}</td><td>{p.quantitySold}</td><td>{formatMoney(p.revenue)}</td></tr>
                ))}
              </Table>
            </Card>
            <Card>
              <h3>Meilleurs clients</h3>
              <Table headers={['Client', 'Achats', 'Total']}>
                {period.topCustomers.map((c) => (
                  <tr key={c.customerId}><td>{c.customerName}</td><td>{c.purchaseCount}</td><td>{formatMoney(c.totalSpent)}</td></tr>
                ))}
              </Table>
            </Card>
          </div>
        </>
      )}
    </>
  )
}
