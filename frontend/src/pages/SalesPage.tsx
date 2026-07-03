import { useEffect, useState } from 'react'
import { salesApi } from '../api/client'
import { formatMoney, formatDate, PAYMENT_MODES, SALE_STATUS } from '../api/types'
import type { Sale } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal, Field } from '../components/Layout'
import { openReceiptPrint } from '../utils/printReceipt'

export default function SalesPage() {
  const [sales, setSales] = useState<Sale[]>([])
  const [error, setError] = useState('')
  const [cancelId, setCancelId] = useState<number | null>(null)
  const [reason, setReason] = useState('')

  const load = () => salesApi.list().then(setSales).catch((e) => setError(e.message))
  useEffect(() => { load() }, [])

  const handleCancel = async () => {
    if (!cancelId) return
    try {
      await salesApi.cancel(cancelId, reason)
      setCancelId(null)
      setReason('')
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erreur')
    }
  }

  const handlePrint = async (saleId: number) => {
    try {
      await openReceiptPrint(saleId)
    } catch {
      setError('Erreur impression — autorisez les popups')
    }
  }

  return (
    <>
      <PageHeader title="Historique des ventes" subtitle="Consultez, imprimez et annulez les ventes" />
      {error && <Alert type="error">{error}</Alert>}
      <Card>
        <Table headers={['#', 'Date', 'Client', 'Vendeur', 'Total', 'Paiement', 'Statut', 'Facture', 'Actions']}>
          {sales.map((s) => (
            <tr key={s.id}>
              <td>{s.id}</td>
              <td>{formatDate(s.soldAt)}</td>
              <td>{s.customerName}</td>
              <td>{s.employeeName || '—'}</td>
              <td>{formatMoney(s.totalAmount)}</td>
              <td>{PAYMENT_MODES[s.paymentMode]}</td>
              <td><span className={`badge badge-${s.status === 1 ? 'ok' : 'danger'}`}>{SALE_STATUS[s.status]}</span></td>
              <td>{s.invoiceNumber || '—'}</td>
              <td className="actions-cell">
                {s.status === 1 && (
                  <>
                    <Btn variant="ghost" onClick={() => handlePrint(s.id)}>🖨️ Reçu</Btn>
                    <Btn variant="danger" onClick={() => setCancelId(s.id)}>Annuler</Btn>
                  </>
                )}
              </td>
            </tr>
          ))}
        </Table>
      </Card>

      <Modal open={!!cancelId} onClose={() => setCancelId(null)} title="Annuler la vente">
        <Field label="Motif d'annulation">
          <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} />
        </Field>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setCancelId(null)}>Fermer</Btn>
          <Btn variant="danger" onClick={handleCancel} disabled={!reason}>Confirmer</Btn>
        </div>
      </Modal>
    </>
  )
}
