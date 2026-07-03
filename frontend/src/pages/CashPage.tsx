import { useEffect, useState } from 'react'
import { cashApi, personnelApi } from '../api/client'
import { formatMoney, formatDate } from '../api/types'
import type { CashBox, CashTransaction, Employee } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal, Field, StatCard } from '../components/Layout'
import { useAuth } from '../context/AuthContext'

const EXPENSE_MOTIVES = ['Repas', 'Transport', 'Carburant', 'Thé/Café', 'Fournitures', 'Dépense diverse']

export default function CashPage() {
  const { user } = useAuth()
  const [boxes, setBoxes] = useState<CashBox[]>([])
  const [expenses, setExpenses] = useState<CashTransaction[]>([])
  const [employees, setEmployees] = useState<Employee[]>([])
  const [modal, setModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [form, setForm] = useState({ cashBoxId: 1, employeeId: 0, amount: 0, motive: 'Repas' })

  const load = async () => {
    const [b, e, emp] = await Promise.all([cashApi.boxes(), cashApi.expenses(), personnelApi.employees()])
    setBoxes(b)
    setExpenses(e)
    setEmployees(emp)
    if (b.length) setForm((f) => ({ ...f, cashBoxId: b[0].id }))
    if (user?.employeeId) setForm((f) => ({ ...f, employeeId: user.employeeId! }))
  }

  useEffect(() => { load().catch((e) => setError(e.message)) }, [])

  const openCreate = () => {
    setEditingId(null)
    setForm({ cashBoxId: boxes[0]?.id ?? 1, employeeId: user?.employeeId ?? employees[0]?.id ?? 0, amount: 0, motive: 'Repas' })
    setModal(true)
  }

  const openEdit = (e: CashTransaction) => {
    setEditingId(e.id)
    setForm({ cashBoxId: e.cashBoxId, employeeId: e.employeeId ?? 0, amount: e.amount, motive: e.motive })
    setModal(true)
  }

  const save = async () => {
    try {
      if (editingId) {
        await cashApi.updateExpense(editingId, { ...form, employeeId: form.employeeId || user?.employeeId })
        setSuccess('Dépense modifiée')
      } else {
        await cashApi.addExpense({ ...form, employeeId: form.employeeId || user?.employeeId })
        setSuccess('Dépense enregistrée')
      }
      setModal(false)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const confirmDelete = async () => {
    if (!deleteId) return
    try {
      await cashApi.deleteExpense(deleteId)
      setSuccess('Dépense supprimée')
      setDeleteId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const balance = boxes[0]?.currentBalance ?? 0

  return (
    <>
      <PageHeader title="Caisse & Dépenses" subtitle="Gérer les sorties de caisse" action={<Btn onClick={openCreate}>+ Nouvelle dépense</Btn>} />
      {error && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}
      <div className="stats-grid">
        <StatCard label="Solde caisse" value={formatMoney(balance)} tone="cash" />
        <StatCard label="Dépenses enregistrées" value={String(expenses.length)} tone="expense" />
      </div>
      <Card>
        <Table headers={['Date', 'Caisse', 'Montant', 'Motif', 'Employé', 'Actions']}>
          {expenses.map((e) => (
            <tr key={e.id}>
              <td>{formatDate(e.createdAt)}</td>
              <td>{e.cashBoxName}</td>
              <td className="text-danger">{formatMoney(e.amount)}</td>
              <td>{e.motive}</td>
              <td>{e.employeeName || '—'}</td>
              <td className="actions-cell">
                <Btn variant="ghost" onClick={() => openEdit(e)}>Modifier</Btn>
                <Btn variant="danger" onClick={() => setDeleteId(e.id)}>Supprimer</Btn>
              </td>
            </tr>
          ))}
        </Table>
      </Card>

      <Modal open={modal} onClose={() => setModal(false)} title={editingId ? 'Modifier la dépense' : 'Nouvelle dépense'}>
        <Field label="Caisse">
          <select value={form.cashBoxId} onChange={(e) => setForm({ ...form, cashBoxId: +e.target.value })}>
            {boxes.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </Field>
        <Field label="Motif">
          <select value={form.motive} onChange={(e) => setForm({ ...form, motive: e.target.value })}>
            {EXPENSE_MOTIVES.map((m) => <option key={m} value={m}>{m}</option>)}
          </select>
        </Field>
        <Field label="Montant"><input type="number" value={form.amount} onChange={(e) => setForm({ ...form, amount: +e.target.value })} /></Field>
        <Field label="Employé responsable">
          <select value={form.employeeId} onChange={(e) => setForm({ ...form, employeeId: +e.target.value })}>
            {employees.map((emp) => <option key={emp.id} value={emp.id}>{emp.name}</option>)}
          </select>
        </Field>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setModal(false)}>Annuler</Btn>
          <Btn onClick={save}>Enregistrer</Btn>
        </div>
      </Modal>

      <Modal open={!!deleteId} onClose={() => setDeleteId(null)} title="Supprimer la dépense">
        <p>Confirmer la suppression de cette dépense ?</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDelete}>Supprimer</Btn>
        </div>
      </Modal>
    </>
  )
}
