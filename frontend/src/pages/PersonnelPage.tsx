import { useEffect, useState } from 'react'
import { personnelApi } from '../api/client'
import { formatMoney, formatDate } from '../api/types'
import type { Employee, EmployeeRole } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal, Field } from '../components/Layout'

const emptyEmp = { name: '', phone: '', email: '', roleId: 1, monthlySalary: 150000 }

export default function PersonnelPage() {
  const [tab, setTab] = useState<'employees' | 'roles' | 'attendance' | 'advances'>('employees')
  const [employees, setEmployees] = useState<Employee[]>([])
  const [roles, setRoles] = useState<EmployeeRole[]>([])
  const [attendance, setAttendance] = useState<{ id: number; employeeId: number; employeeName: string; date: string; present: boolean; note?: string }[]>([])
  const [advances, setAdvances] = useState<{ id: number; employeeId: number; employeeName: string; amount: number; createdAt: string; motive: string | null }[]>([])
  const [modal, setModal] = useState(false)
  const [editingEmpId, setEditingEmpId] = useState<number | null>(null)
  const [deleteEmpId, setDeleteEmpId] = useState<number | null>(null)
  const [editRole, setEditRole] = useState<EmployeeRole | null>(null)
  const [deleteRoleId, setDeleteRoleId] = useState<number | null>(null)
  const [newRole, setNewRole] = useState('')
  const [deleteAttId, setDeleteAttId] = useState<number | null>(null)
  const [deleteAdvId, setDeleteAdvId] = useState<number | null>(null)
  const [editAtt, setEditAtt] = useState<{ id: number; employeeId: number; date: string; present: boolean } | null>(null)
  const [editAdv, setEditAdv] = useState<{ id: number; employeeId: number; amount: number; motive: string } | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [form, setForm] = useState(emptyEmp)

  const load = async () => {
    const [emp, r, att, adv] = await Promise.all([
      personnelApi.employees(), personnelApi.roles(),
      personnelApi.attendance(), personnelApi.advances(),
    ])
    setEmployees(emp)
    setRoles(r)
    setAttendance(att)
    setAdvances(adv)
    if (r.length) setForm((f) => ({ ...f, roleId: r[0].id }))
  }

  useEffect(() => { load().catch((e) => setError(e.message)) }, [])

  const openCreateEmp = () => { setEditingEmpId(null); setForm({ ...emptyEmp, roleId: roles[0]?.id ?? 1 }); setModal(true) }
  const openEditEmp = (e: Employee) => {
    setEditingEmpId(e.id)
    setForm({ name: e.name, phone: e.phone || '', email: e.email || '', roleId: e.roleId, monthlySalary: e.monthlySalary })
    setModal(true)
  }

  const saveEmployee = async () => {
    try {
      if (editingEmpId) {
        await personnelApi.updateEmployee(editingEmpId, form)
        setSuccess('Employé modifié')
      } else {
        await personnelApi.createEmployee(form)
        setSuccess('Employé ajouté')
      }
      setModal(false)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const confirmDeleteEmp = async () => {
    if (!deleteEmpId) return
    try {
      await personnelApi.deleteEmployee(deleteEmpId)
      setSuccess('Employé désactivé')
      setDeleteEmpId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const addRole = async () => {
    if (!newRole) return
    await personnelApi.createRole(newRole)
    setNewRole('')
    load()
  }

  const saveRole = async () => {
    if (!editRole) return
    await personnelApi.updateRole(editRole.id, editRole.name)
    setEditRole(null)
    load()
  }

  const confirmDeleteRole = async () => {
    if (!deleteRoleId) return
    try {
      await personnelApi.deleteRole(deleteRoleId)
      setDeleteRoleId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const markPresent = async (employeeId: number) => {
    const today = new Date().toISOString().slice(0, 10)
    await personnelApi.addAttendance({ employeeId, date: today, present: true })
    load()
  }

  const addAdvance = async (employeeId: number) => {
    const amount = prompt('Montant de l\'avance :')
    if (!amount) return
    await personnelApi.addAdvance({ employeeId, amount: +amount, motive: 'Avance sur salaire' })
    load()
  }

  const confirmDeleteAtt = async () => {
    if (!deleteAttId) return
    await personnelApi.deleteAttendance(deleteAttId)
    setDeleteAttId(null)
    load()
  }

  const saveAttendance = async () => {
    if (!editAtt) return
    await personnelApi.updateAttendance(editAtt.id, { employeeId: editAtt.employeeId, date: editAtt.date, present: editAtt.present })
    setEditAtt(null)
    load()
  }

  const confirmDeleteAdv = async () => {
    if (!deleteAdvId) return
    await personnelApi.deleteAdvance(deleteAdvId)
    setDeleteAdvId(null)
    load()
  }

  const saveAdvance = async () => {
    if (!editAdv) return
    await personnelApi.updateAdvance(editAdv.id, { employeeId: editAdv.employeeId, amount: editAdv.amount, motive: editAdv.motive })
    setEditAdv(null)
    load()
  }

  return (
    <>
      <PageHeader title="Personnel" subtitle="Gérer employés, rôles, présences et avances" action={tab === 'employees' ? <Btn onClick={openCreateEmp}>+ Employé</Btn> : undefined} />
      {error && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}
      <div className="tabs">
        <button className={tab === 'employees' ? 'tab active' : 'tab'} onClick={() => setTab('employees')}>Employés</button>
        <button className={tab === 'roles' ? 'tab active' : 'tab'} onClick={() => setTab('roles')}>Rôles</button>
        <button className={tab === 'attendance' ? 'tab active' : 'tab'} onClick={() => setTab('attendance')}>Présences</button>
        <button className={tab === 'advances' ? 'tab active' : 'tab'} onClick={() => setTab('advances')}>Avances</button>
      </div>

      {tab === 'employees' && (
        <Card>
          <Table headers={['Nom', 'Rôle', 'Salaire', 'Téléphone', 'Statut', 'Actions']}>
            {employees.map((e) => (
              <tr key={e.id}>
                <td>{e.name}</td>
                <td>{e.roleName}</td>
                <td>{formatMoney(e.monthlySalary)}</td>
                <td>{e.phone || '—'}</td>
                <td>{e.isActive ? 'Actif' : 'Inactif'}</td>
                <td className="actions-cell">
                  <Btn variant="ghost" onClick={() => openEditEmp(e)}>Modifier</Btn>
                  <Btn variant="ghost" onClick={() => markPresent(e.id)}>Présent</Btn>
                  <Btn variant="ghost" onClick={() => addAdvance(e.id)}>Avance</Btn>
                  <Btn variant="danger" onClick={() => setDeleteEmpId(e.id)}>Supprimer</Btn>
                </td>
              </tr>
            ))}
          </Table>
        </Card>
      )}

      {tab === 'roles' && (
        <Card>
          <div className="inline-form">
            <input placeholder="Nouveau rôle" value={newRole} onChange={(e) => setNewRole(e.target.value)} />
            <Btn onClick={addRole}>Ajouter</Btn>
          </div>
          <Table headers={['Rôle', 'Actions']}>
            {roles.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td className="actions-cell">
                  <Btn variant="ghost" onClick={() => setEditRole({ ...r })}>Modifier</Btn>
                  <Btn variant="danger" onClick={() => setDeleteRoleId(r.id)}>Supprimer</Btn>
                </td>
              </tr>
            ))}
          </Table>
        </Card>
      )}

      {tab === 'attendance' && (
        <Card>
          <Table headers={['Employé', 'Date', 'Présent', 'Actions']}>
            {attendance.map((a) => (
              <tr key={a.id}>
                <td>{a.employeeName}</td>
                <td>{a.date}</td>
                <td>{a.present ? '✅' : '❌'}</td>
                <td className="actions-cell">
                  <Btn variant="ghost" onClick={() => setEditAtt({ id: a.id, employeeId: a.employeeId, date: a.date, present: a.present })}>Modifier</Btn>
                  <Btn variant="danger" onClick={() => setDeleteAttId(a.id)}>Supprimer</Btn>
                </td>
              </tr>
            ))}
          </Table>
        </Card>
      )}

      {tab === 'advances' && (
        <Card>
          <Table headers={['Employé', 'Montant', 'Date', 'Motif', 'Actions']}>
            {advances.map((a) => (
              <tr key={a.id}>
                <td>{a.employeeName}</td>
                <td>{formatMoney(a.amount)}</td>
                <td>{formatDate(a.createdAt)}</td>
                <td>{a.motive || '—'}</td>
                <td className="actions-cell">
                  <Btn variant="ghost" onClick={() => setEditAdv({ id: a.id, employeeId: a.employeeId, amount: a.amount, motive: a.motive || '' })}>Modifier</Btn>
                  <Btn variant="danger" onClick={() => setDeleteAdvId(a.id)}>Supprimer</Btn>
                </td>
              </tr>
            ))}
          </Table>
        </Card>
      )}

      <Modal open={modal} onClose={() => setModal(false)} title={editingEmpId ? 'Modifier l\'employé' : 'Nouvel employé'}>
        <Field label="Nom"><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="Téléphone"><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></Field>
        <Field label="Email"><input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></Field>
        <Field label="Rôle">
          <select value={form.roleId} onChange={(e) => setForm({ ...form, roleId: +e.target.value })}>
            {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
          </select>
        </Field>
        <Field label="Salaire mensuel"><input type="number" value={form.monthlySalary} onChange={(e) => setForm({ ...form, monthlySalary: +e.target.value })} /></Field>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setModal(false)}>Annuler</Btn>
          <Btn onClick={saveEmployee}>Enregistrer</Btn>
        </div>
      </Modal>

      <Modal open={!!deleteEmpId} onClose={() => setDeleteEmpId(null)} title="Supprimer l'employé">
        <p>Désactiver cet employé ?</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteEmpId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDeleteEmp}>Supprimer</Btn>
        </div>
      </Modal>

      <Modal open={!!editRole} onClose={() => setEditRole(null)} title="Modifier le rôle">
        <Field label="Nom"><input value={editRole?.name || ''} onChange={(e) => setEditRole((r) => r ? { ...r, name: e.target.value } : null)} /></Field>
        <div className="modal-actions"><Btn onClick={saveRole}>Enregistrer</Btn></div>
      </Modal>

      <Modal open={!!deleteRoleId} onClose={() => setDeleteRoleId(null)} title="Supprimer le rôle">
        <p>Supprimer ce rôle ?</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteRoleId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDeleteRole}>Supprimer</Btn>
        </div>
      </Modal>

      <Modal open={!!editAtt} onClose={() => setEditAtt(null)} title="Modifier la présence">
        <Field label="Employé">
          <select value={editAtt?.employeeId} onChange={(e) => setEditAtt((a) => a ? { ...a, employeeId: +e.target.value } : null)}>
            {employees.map((e) => <option key={e.id} value={e.id}>{e.name}</option>)}
          </select>
        </Field>
        <Field label="Date"><input type="date" value={editAtt?.date} onChange={(e) => setEditAtt((a) => a ? { ...a, date: e.target.value } : null)} /></Field>
        <Field label="Présent">
          <select value={editAtt?.present ? '1' : '0'} onChange={(e) => setEditAtt((a) => a ? { ...a, present: e.target.value === '1' } : null)}>
            <option value="1">Oui</option>
            <option value="0">Non</option>
          </select>
        </Field>
        <div className="modal-actions"><Btn onClick={saveAttendance}>Enregistrer</Btn></div>
      </Modal>

      <Modal open={!!editAdv} onClose={() => setEditAdv(null)} title="Modifier l'avance">
        <Field label="Employé">
          <select value={editAdv?.employeeId} onChange={(e) => setEditAdv((a) => a ? { ...a, employeeId: +e.target.value } : null)}>
            {employees.map((e) => <option key={e.id} value={e.id}>{e.name}</option>)}
          </select>
        </Field>
        <Field label="Montant"><input type="number" value={editAdv?.amount} onChange={(e) => setEditAdv((a) => a ? { ...a, amount: +e.target.value } : null)} /></Field>
        <Field label="Motif"><input value={editAdv?.motive} onChange={(e) => setEditAdv((a) => a ? { ...a, motive: e.target.value } : null)} /></Field>
        <div className="modal-actions"><Btn onClick={saveAdvance}>Enregistrer</Btn></div>
      </Modal>

      <Modal open={!!deleteAttId} onClose={() => setDeleteAttId(null)} title="Supprimer la présence">
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteAttId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDeleteAtt}>Supprimer</Btn>
        </div>
      </Modal>

      <Modal open={!!deleteAdvId} onClose={() => setDeleteAdvId(null)} title="Supprimer l'avance">
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteAdvId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDeleteAdv}>Supprimer</Btn>
        </div>
      </Modal>
    </>
  )
}
