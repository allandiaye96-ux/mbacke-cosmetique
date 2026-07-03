import { useEffect, useState } from 'react'
import { purchasesApi, suppliersApi, productsApi } from '../api/client'
import { formatMoney, formatDate } from '../api/types'
import type { Purchase, Supplier, Product } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal, Field } from '../components/Layout'

type LineForm = { productId: number; quantity: number; unitPurchasePrice: number; expirationDate: string }

const defaultLine = (products: Product[]): LineForm => ({
  productId: products[0]?.id || 1,
  quantity: 10,
  unitPurchasePrice: products[0]?.purchasePrice || 0,
  expirationDate: new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10),
})

export default function PurchasesPage() {
  const [purchases, setPurchases] = useState<Purchase[]>([])
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [modal, setModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [cancelId, setCancelId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [supplierId, setSupplierId] = useState(1)
  const [lines, setLines] = useState<LineForm[]>([defaultLine([])])

  const load = async () => {
    const [p, s, pr] = await Promise.all([purchasesApi.list(), suppliersApi.list(), productsApi.list()])
    setPurchases(p)
    setSuppliers(s)
    setProducts(pr)
    if (s.length) setSupplierId(s[0].id)
  }

  useEffect(() => { load().catch((e) => setError(e.message)) }, [])

  const openCreate = () => {
    setEditingId(null)
    setSupplierId(suppliers[0]?.id ?? 1)
    setLines([defaultLine(products)])
    setModal(true)
  }

  const openEdit = (p: Purchase) => {
    setEditingId(p.id)
    setSupplierId(p.supplierId)
    setLines(p.lines.map((l) => ({
      productId: l.productId,
      quantity: l.quantity,
      unitPurchasePrice: l.unitPurchasePrice,
      expirationDate: l.expirationDate,
    })))
    setModal(true)
  }

  const save = async () => {
    try {
      if (editingId) {
        await purchasesApi.update(editingId, { supplierId, lines })
        setSuccess('Achat modifié')
      } else {
        await purchasesApi.create({ supplierId, lines })
        setSuccess('Achat enregistré')
      }
      setModal(false)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const confirmCancel = async () => {
    if (!cancelId) return
    try {
      await purchasesApi.cancel(cancelId)
      setSuccess('Achat annulé')
      setCancelId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const statusLabel = (s: number) => s === 2 ? 'Livré' : s === 1 ? 'En attente' : 'Annulé'

  return (
    <>
      <PageHeader title="Achats & Livraisons" subtitle="Historique des achats fournisseurs" action={<Btn onClick={openCreate}>+ Nouvel achat</Btn>} />
      {error && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}
      <Card>
        <Table headers={['#', 'Date', 'Fournisseur', 'Total', 'Payé', 'Statut', 'Lignes', 'Actions']}>
          {purchases.map((p) => (
            <tr key={p.id}>
              <td>{p.id}</td>
              <td>{formatDate(p.purchasedAt)}</td>
              <td>{p.supplierName}</td>
              <td>{formatMoney(p.totalAmount)}</td>
              <td>{formatMoney(p.amountPaid)}</td>
              <td>{statusLabel(p.status)}</td>
              <td>{p.lines.length} article(s)</td>
              <td className="actions-cell">
                {p.status !== 3 && (
                  <>
                    <Btn variant="ghost" onClick={() => openEdit(p)} disabled={p.amountPaid > 0}>Modifier</Btn>
                    <Btn variant="danger" onClick={() => setCancelId(p.id)} disabled={p.amountPaid > 0}>Annuler</Btn>
                  </>
                )}
              </td>
            </tr>
          ))}
        </Table>
      </Card>

      <Modal open={modal} onClose={() => setModal(false)} title={editingId ? 'Modifier l\'achat' : 'Nouvel achat'}>
        <Field label="Fournisseur">
          <select value={supplierId} onChange={(e) => setSupplierId(+e.target.value)}>
            {suppliers.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </Field>
        {lines.map((line, i) => (
          <div key={i} className="field-row">
            <Field label="Produit">
              <select value={line.productId} onChange={(e) => {
                const l = [...lines]; l[i].productId = +e.target.value
                const pr = products.find((p) => p.id === +e.target.value)
                if (pr) l[i].unitPurchasePrice = pr.purchasePrice
                setLines(l)
              }}>
                {products.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </Field>
            <Field label="Qté"><input type="number" value={line.quantity} onChange={(e) => { const l = [...lines]; l[i].quantity = +e.target.value; setLines(l) }} /></Field>
            <Field label="Prix"><input type="number" value={line.unitPurchasePrice} onChange={(e) => { const l = [...lines]; l[i].unitPurchasePrice = +e.target.value; setLines(l) }} /></Field>
            <Field label="Expiration"><input type="date" value={line.expirationDate} onChange={(e) => { const l = [...lines]; l[i].expirationDate = e.target.value; setLines(l) }} /></Field>
          </div>
        ))}
        <Btn variant="ghost" onClick={() => setLines([...lines, defaultLine(products)])}>+ Ligne</Btn>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setModal(false)}>Annuler</Btn>
          <Btn onClick={save}>Enregistrer</Btn>
        </div>
      </Modal>

      <Modal open={!!cancelId} onClose={() => setCancelId(null)} title="Annuler l'achat">
        <p>Annuler cet achat et retirer les quantités du stock ?</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setCancelId(null)}>Fermer</Btn>
          <Btn variant="danger" onClick={confirmCancel}>Confirmer</Btn>
        </div>
      </Modal>
    </>
  )
}
