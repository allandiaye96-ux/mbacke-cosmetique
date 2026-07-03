import { useEffect, useState } from 'react'
import { productsApi, catalogApi } from '../api/client'
import { formatMoney } from '../api/types'
import type { Product, Category, Brand } from '../api/types'
import { PageHeader, Card, Table, Btn, Alert, Modal, Field } from '../components/Layout'

const emptyForm = {
  name: '', sku: '', categoryId: 1, brandId: 1,
  purchasePrice: 0, salePrice: 0, minStockLevel: 5, isActive: true,
}

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [brands, setBrands] = useState<Brand[]>([])
  const [tab, setTab] = useState<'products' | 'categories' | 'brands'>('products')
  const [modal, setModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [form, setForm] = useState(emptyForm)
  const [newCat, setNewCat] = useState('')
  const [newBrand, setNewBrand] = useState('')
  const [editCat, setEditCat] = useState<Category | null>(null)
  const [editBrand, setEditBrand] = useState<Brand | null>(null)
  const [deleteCatId, setDeleteCatId] = useState<number | null>(null)
  const [deleteBrandId, setDeleteBrandId] = useState<number | null>(null)

  const load = async () => {
    const [p, c, b] = await Promise.all([productsApi.list(), catalogApi.categories(), catalogApi.brands()])
    setProducts(p)
    setCategories(c)
    setBrands(b)
    if (c.length && !editingId) setForm((f) => ({ ...f, categoryId: c[0].id }))
    if (b.length && !editingId) setForm((f) => ({ ...f, brandId: b[0].id }))
  }

  useEffect(() => { load().catch((e) => setError(e.message)) }, [])

  const openCreate = () => {
    setEditingId(null)
    setForm({ ...emptyForm, categoryId: categories[0]?.id ?? 1, brandId: brands[0]?.id ?? 1 })
    setModal(true)
  }

  const openEdit = (p: Product) => {
    setEditingId(p.id)
    setForm({
      name: p.name,
      sku: p.sku || '',
      categoryId: p.categoryId,
      brandId: p.brandId,
      purchasePrice: p.purchasePrice,
      salePrice: p.salePrice,
      minStockLevel: p.minStockLevel,
      isActive: p.isActive,
    })
    setModal(true)
  }

  const saveProduct = async () => {
    if (!form.name.trim()) { setError('Le nom est obligatoire'); return }
    try {
      setError('')
      if (editingId) {
        await productsApi.update(editingId, form)
        setSuccess('Produit modifié avec succès')
      } else {
        await productsApi.create(form)
        setSuccess('Produit ajouté avec succès')
      }
      setModal(false)
      setEditingId(null)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erreur')
    }
  }

  const confirmDelete = async () => {
    if (!deleteId) return
    try {
      await productsApi.delete(deleteId)
      setSuccess('Produit supprimé')
      setDeleteId(null)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erreur')
    }
  }

  const addCategory = async () => {
    if (!newCat) return
    await catalogApi.createCategory(newCat)
    setNewCat('')
    load()
  }

  const addBrand = async () => {
    if (!newBrand) return
    await catalogApi.createBrand(newBrand)
    setNewBrand('')
    load()
  }

  const saveCategory = async () => {
    if (!editCat) return
    await catalogApi.updateCategory(editCat.id, editCat.name)
    setEditCat(null)
    load()
  }

  const saveBrand = async () => {
    if (!editBrand) return
    await catalogApi.updateBrand(editBrand.id, editBrand.name)
    setEditBrand(null)
    load()
  }

  const confirmDeleteCat = async () => {
    if (!deleteCatId) return
    try {
      await catalogApi.deleteCategory(deleteCatId)
      setDeleteCatId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  const confirmDeleteBrand = async () => {
    if (!deleteBrandId) return
    try {
      await catalogApi.deleteBrand(deleteBrandId)
      setDeleteBrandId(null)
      load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Erreur') }
  }

  return (
    <>
      <PageHeader
        title="Produits cosmétiques"
        subtitle="Ajouter, modifier ou supprimer des produits"
        action={tab === 'products' ? <Btn onClick={openCreate}>+ Nouveau produit</Btn> : undefined}
      />
      {error && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}

      <div className="tabs">
        {(['products', 'categories', 'brands'] as const).map((t) => (
          <button key={t} className={tab === t ? 'tab active' : 'tab'} onClick={() => setTab(t)}>
            {t === 'products' ? 'Produits' : t === 'categories' ? 'Catégories' : 'Marques'}
          </button>
        ))}
      </div>

      {tab === 'products' && (
        <Card>
          <Table headers={['Nom', 'SKU', 'Catégorie', 'Marque', 'Achat', 'Vente', 'Stock', 'Min', 'Actions']}>
            {products.map((p) => (
              <tr key={p.id} className={p.stockQuantity <= p.minStockLevel ? 'row-warning' : ''}>
                <td>{p.name}</td>
                <td>{p.sku}</td>
                <td>{p.categoryName}</td>
                <td>{p.brandName}</td>
                <td>{formatMoney(p.purchasePrice)}</td>
                <td>{formatMoney(p.salePrice)}</td>
                <td className={p.stockQuantity <= p.minStockLevel ? 'text-danger' : ''}>{p.stockQuantity}</td>
                <td>{p.minStockLevel}</td>
                <td className="actions-cell">
                  <Btn variant="ghost" onClick={() => openEdit(p)}>Modifier</Btn>
                  <Btn variant="danger" onClick={() => setDeleteId(p.id)}>Supprimer</Btn>
                </td>
              </tr>
            ))}
          </Table>
        </Card>
      )}

      {tab === 'categories' && (
        <Card>
          <div className="inline-form">
            <input placeholder="Nouvelle catégorie" value={newCat} onChange={(e) => setNewCat(e.target.value)} />
            <Btn onClick={addCategory}>Ajouter</Btn>
          </div>
          <Table headers={['Nom', 'Actions']}>
            {categories.map((c) => (
              <tr key={c.id}>
                <td>{c.name}</td>
                <td className="actions-cell">
                  <Btn variant="ghost" onClick={() => setEditCat({ ...c })}>Modifier</Btn>
                  <Btn variant="danger" onClick={() => setDeleteCatId(c.id)}>Supprimer</Btn>
                </td>
              </tr>
            ))}
          </Table>
        </Card>
      )}

      {tab === 'brands' && (
        <Card>
          <div className="inline-form">
            <input placeholder="Nouvelle marque" value={newBrand} onChange={(e) => setNewBrand(e.target.value)} />
            <Btn onClick={addBrand}>Ajouter</Btn>
          </div>
          <Table headers={['Nom', 'Actions']}>
            {brands.map((b) => (
              <tr key={b.id}>
                <td>{b.name}</td>
                <td className="actions-cell">
                  <Btn variant="ghost" onClick={() => setEditBrand({ ...b })}>Modifier</Btn>
                  <Btn variant="danger" onClick={() => setDeleteBrandId(b.id)}>Supprimer</Btn>
                </td>
              </tr>
            ))}
          </Table>
        </Card>
      )}

      <Modal open={modal} onClose={() => { setModal(false); setEditingId(null) }} title={editingId ? 'Modifier le produit' : 'Nouveau produit'}>
        <Field label="Nom *"><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="SKU"><input value={form.sku} onChange={(e) => setForm({ ...form, sku: e.target.value })} /></Field>
        <Field label="Catégorie">
          <select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: +e.target.value })}>
            {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </Field>
        <Field label="Marque">
          <select value={form.brandId} onChange={(e) => setForm({ ...form, brandId: +e.target.value })}>
            {brands.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </Field>
        <div className="field-row">
          <Field label="Prix achat (F)"><input type="number" min={0} value={form.purchasePrice} onChange={(e) => setForm({ ...form, purchasePrice: +e.target.value })} /></Field>
          <Field label="Prix vente (F)"><input type="number" min={0} value={form.salePrice} onChange={(e) => setForm({ ...form, salePrice: +e.target.value })} /></Field>
        </div>
        <Field label="Stock minimum"><input type="number" min={0} value={form.minStockLevel} onChange={(e) => setForm({ ...form, minStockLevel: +e.target.value })} /></Field>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => { setModal(false); setEditingId(null) }}>Annuler</Btn>
          <Btn onClick={saveProduct}>{editingId ? 'Enregistrer' : 'Ajouter'}</Btn>
        </div>
      </Modal>

      <Modal open={!!deleteId} onClose={() => setDeleteId(null)} title="Supprimer le produit">
        <p>Confirmez-vous la suppression de ce produit ? Il ne sera plus visible dans le catalogue.</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDelete}>Supprimer</Btn>
        </div>
      </Modal>

      <Modal open={!!editCat} onClose={() => setEditCat(null)} title="Modifier la catégorie">
        <Field label="Nom"><input value={editCat?.name || ''} onChange={(e) => setEditCat((c) => c ? { ...c, name: e.target.value } : null)} /></Field>
        <div className="modal-actions"><Btn onClick={saveCategory}>Enregistrer</Btn></div>
      </Modal>

      <Modal open={!!editBrand} onClose={() => setEditBrand(null)} title="Modifier la marque">
        <Field label="Nom"><input value={editBrand?.name || ''} onChange={(e) => setEditBrand((b) => b ? { ...b, name: e.target.value } : null)} /></Field>
        <div className="modal-actions"><Btn onClick={saveBrand}>Enregistrer</Btn></div>
      </Modal>

      <Modal open={!!deleteCatId} onClose={() => setDeleteCatId(null)} title="Supprimer la catégorie">
        <p>Supprimer cette catégorie ?</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteCatId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDeleteCat}>Supprimer</Btn>
        </div>
      </Modal>

      <Modal open={!!deleteBrandId} onClose={() => setDeleteBrandId(null)} title="Supprimer la marque">
        <p>Supprimer cette marque ?</p>
        <div className="modal-actions">
          <Btn variant="ghost" onClick={() => setDeleteBrandId(null)}>Annuler</Btn>
          <Btn variant="danger" onClick={confirmDeleteBrand}>Supprimer</Btn>
        </div>
      </Modal>
    </>
  )
}
