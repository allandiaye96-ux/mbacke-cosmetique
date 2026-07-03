import { useEffect, useState } from 'react'
import { invoicesApi } from '../api/client'
import { formatMoney, formatDate } from '../api/types'
import type { Invoice } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal } from '../components/Layout'

export default function InvoicesPage() {
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [cancelId, setCancelId] = useState<number | null>(null)

  const load = () => invoicesApi.list().then(setInvoices).catch((e) => setError(e.message))
  useEffect(() => { load() }, [])

  const download = async (id: number, number: string) => {
    try {
      const blob = await invoicesApi.downloadPdf(id)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${number}.pdf`
      a.click()
      URL.revokeObjectURL(url)
    } catch {
      setError('Erreur téléchargement PDF')
    }
  }

  const confirmCancel = async () => {
    if (!cancelId) return
    try {
      await invoicesApi.cancel(cancelId)
      setSuccess('Facture annulée')
      setCancelId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const statusLabel = (s: number) => ['Brouillon', 'Émise', 'Payée', 'Annulée'][s - 1] || '?'

  return (
    <>
      <PageHeader title="Facturation" subtitle="Factures générées automatiquement à chaque vente" />
      {error && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}
      <Card>
        <Table headers={['N° Facture', 'Date', 'Client', 'Vente', 'Total', 'Statut', 'Actions']}>
          {invoices.map((inv) => (
            <tr key={inv.id}>
              <td><strong>{inv.invoiceNumber}</strong></td>
              <td>{formatDate(inv.issuedAt)}</td>
              <td>{inv.customerName}</td>
              <td>{inv.saleId ? `#${inv.saleId}` : '—'}</td>
              <td>{formatMoney(inv.totalAmount)}</td>
              <td><span className={`badge badge-${inv.status === 4 ? 'danger' : 'ok'}`}>{statusLabel(inv.status)}</span></td>
              <td className="actions-cell">
                <Btn variant="ghost" onClick={() => download(inv.id, inv.invoiceNumber)}>🖨️ PDF</Btn>
                {inv.status !== 4 && (
                  <Btn variant="danger" onClick={() => setCancelId(inv.id)}>Annuler</Btn>
                )}
              </td>
            </tr>
          ))}
        </Table>
      </Card>

      <Modal open={!!cancelId} onClose={() => setCancelId(null)} title="Annuler la facture">
        <p>Marquer cette facture comme annulée ?</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setCancelId(null)}>Fermer</Btn>
          <Btn variant="danger" onClick={confirmCancel}>Confirmer</Btn>
        </div>
      </Modal>
    </>
  )
}
