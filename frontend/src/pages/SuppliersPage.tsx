import { useEffect, useState } from 'react'
import { suppliersApi } from '../api/client'
import { formatMoney } from '../api/types'
import type { Supplier } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal, Field } from '../components/Layout'

const emptyForm = { name: '', phone: '', email: '', address: '' }

export default function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [modal, setModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [form, setForm] = useState(emptyForm)

  const load = () => suppliersApi.list().then(setSuppliers).catch((e) => setError(e.message))
  useEffect(() => { load() }, [])

  const openCreate = () => { setEditingId(null); setForm(emptyForm); setModal(true) }

  const openEdit = (s: Supplier) => {
    setEditingId(s.id)
    setForm({ name: s.name, phone: s.phone || '', email: s.email || '', address: s.address || '' })
    setModal(true)
  }

  const save = async () => {
    try {
      if (editingId) {
        await suppliersApi.update(editingId, form)
        setSuccess('Fournisseur modifié')
      } else {
        await suppliersApi.create(form)
        setSuccess('Fournisseur ajouté')
      }
      setModal(false)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const confirmDelete = async () => {
    if (!deleteId) return
    try {
      await suppliersApi.delete(deleteId)
      setSuccess('Fournisseur supprimé')
      setDeleteId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  return (
    <>
      <PageHeader title="Fournisseurs" subtitle="Ajouter, modifier ou supprimer des fournisseurs" action={<Btn onClick={openCreate}>+ Nouveau fournisseur</Btn>} />
      {error && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}
      <Card>
        <Table headers={['Nom', 'Téléphone', 'Email', 'Achats', 'Payé', 'Solde', 'Actions']}>
          {suppliers.map((s) => (
            <tr key={s.id}>
              <td>{s.name}</td>
              <td>{s.phone || '—'}</td>
              <td>{s.email || '—'}</td>
              <td>{formatMoney(s.totalPurchases)}</td>
              <td>{formatMoney(s.totalPaid)}</td>
              <td className={s.balance > 0 ? 'text-danger' : ''}>{formatMoney(s.balance)}</td>
              <td className="actions-cell">
                <Btn variant="ghost" onClick={() => openEdit(s)}>Modifier</Btn>
                <Btn variant="danger" onClick={() => setDeleteId(s.id)}>Supprimer</Btn>
              </td>
            </tr>
          ))}
        </Table>
      </Card>

      <Modal open={modal} onClose={() => setModal(false)} title={editingId ? 'Modifier le fournisseur' : 'Nouveau fournisseur'}>
        <Field label="Nom"><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="Téléphone"><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></Field>
        <Field label="Email"><input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></Field>
        <Field label="Adresse"><input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} /></Field>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setModal(false)}>Annuler</Btn>
          <Btn onClick={save}>Enregistrer</Btn>
        </div>
      </Modal>

      <Modal open={!!deleteId} onClose={() => setDeleteId(null)} title="Supprimer le fournisseur">
        <p>Supprimer ce fournisseur ? Impossible s'il a des achats enregistrés.</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDelete}>Supprimer</Btn>
        </div>
      </Modal>
    </>
  )
}
