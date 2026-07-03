import { useEffect, useState } from 'react'
import { customersApi } from '../api/client'
import { formatMoney } from '../api/types'
import type { Customer } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal, Field } from '../components/Layout'

const emptyForm = { name: '', phone: '', email: '', address: '', isLoyal: false }

export default function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [tab, setTab] = useState<'all' | 'loyal'>('all')
  const [modal, setModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [payModal, setPayModal] = useState<Customer | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [form, setForm] = useState(emptyForm)
  const [payAmount, setPayAmount] = useState(0)

  const load = () => {
    const fn = tab === 'loyal' ? customersApi.loyal : customersApi.list
    fn().then(setCustomers).catch((e) => setError(e.message))
  }
  useEffect(() => { load() }, [tab])

  const openCreate = () => {
    setEditingId(null)
    setForm(emptyForm)
    setModal(true)
  }

  const openEdit = (c: Customer) => {
    setEditingId(c.id)
    setForm({ name: c.name, phone: c.phone || '', email: c.email || '', address: c.address || '', isLoyal: c.isLoyal })
    setModal(true)
  }

  const save = async () => {
    try {
      if (editingId) {
        await customersApi.update(editingId, form)
        setSuccess('Client modifié')
      } else {
        await customersApi.create(form)
        setSuccess('Client ajouté')
      }
      setModal(false)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const confirmDelete = async () => {
    if (!deleteId) return
    try {
      await customersApi.delete(deleteId)
      setSuccess('Client supprimé')
      setDeleteId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const pay = async () => {
    if (!payModal) return
    await customersApi.pay({ customerId: payModal.id, amount: payAmount, paymentMode: 1 })
    setPayModal(null)
    setPayAmount(0)
    load()
  }

  return (
    <>
      <PageHeader title="Clients" subtitle="Ajouter, modifier ou supprimer des clients" action={<Btn onClick={openCreate}>+ Nouveau client</Btn>} />
      {error && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}
      <div className="tabs">
        <button className={tab === 'all' ? 'tab active' : 'tab'} onClick={() => setTab('all')}>Tous</button>
        <button className={tab === 'loyal' ? 'tab active' : 'tab'} onClick={() => setTab('loyal')}>Fidèles</button>
      </div>
      <Card>
        <Table headers={['Nom', 'Téléphone', 'Email', 'Points', 'Fidèle', 'Dette', 'Achats', 'Actions']}>
          {customers.map((c) => (
            <tr key={c.id}>
              <td>{c.name}</td>
              <td>{c.phone || '—'}</td>
              <td>{c.email || '—'}</td>
              <td>{c.loyaltyPoints}</td>
              <td>{c.isLoyal ? '⭐' : '—'}</td>
              <td className={c.totalDebt > 0 ? 'text-danger' : ''}>{formatMoney(c.totalDebt)}</td>
              <td>{c.purchaseCount}</td>
              <td className="actions-cell">
                <Btn variant="ghost" onClick={() => openEdit(c)}>Modifier</Btn>
                {c.totalDebt > 0 && <Btn variant="ghost" onClick={() => setPayModal(c)}>Payer</Btn>}
                <Btn variant="danger" onClick={() => setDeleteId(c.id)}>Supprimer</Btn>
              </td>
            </tr>
          ))}
        </Table>
      </Card>

      <Modal open={modal} onClose={() => setModal(false)} title={editingId ? 'Modifier le client' : 'Nouveau client'}>
        <Field label="Nom"><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="Téléphone"><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></Field>
        <Field label="Email"><input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></Field>
        <Field label="Adresse"><input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} /></Field>
        <label className="checkbox"><input type="checkbox" checked={form.isLoyal} onChange={(e) => setForm({ ...form, isLoyal: e.target.checked })} /> Client fidèle</label>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setModal(false)}>Annuler</Btn>
          <Btn onClick={save}>Enregistrer</Btn>
        </div>
      </Modal>

      <Modal open={!!payModal} onClose={() => setPayModal(null)} title={`Paiement — ${payModal?.name}`}>
        <Field label="Montant"><input type="number" value={payAmount} onChange={(e) => setPayAmount(+e.target.value)} /></Field>
        <div className="modal-actions"><Btn onClick={pay}>Enregistrer paiement</Btn></div>
      </Modal>

      <Modal open={!!deleteId} onClose={() => setDeleteId(null)} title="Supprimer le client">
        <p>Supprimer ce client ? Impossible s'il a des ventes enregistrées.</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDelete}>Supprimer</Btn>
        </div>
      </Modal>
    </>
  )
}
