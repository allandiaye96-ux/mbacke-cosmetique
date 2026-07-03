import { useEffect, useState } from 'react'
import { productsApi, customersApi, salesApi } from '../api/client'
import { formatMoney } from '../api/types'
import type { Product, Customer } from '../api/types'
import { PageHeader, Card, Btn, Alert } from '../components/Layout'
import { useAuth } from '../context/AuthContext'
import { openReceiptPrint } from '../utils/printReceipt'

interface CartItem {
  product: Product
  quantity: number
}

export default function PosPage() {
  const { user } = useAuth()
  const [products, setProducts] = useState<Product[]>([])
  const [customers, setCustomers] = useState<Customer[]>([])
  const [cart, setCart] = useState<CartItem[]>([])
  const [search, setSearch] = useState('')
  const [customerId, setCustomerId] = useState(1)
  const [paymentMode, setPaymentMode] = useState(1)
  const [discountPercent, setDiscountPercent] = useState(0)
  const [discountAmount, setDiscountAmount] = useState(0)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [lastSaleId, setLastSaleId] = useState<number | null>(null)

  useEffect(() => {
    Promise.all([productsApi.list(), customersApi.list()])
      .then(([p, c]) => {
        setProducts(p.filter((x) => x.stockQuantity > 0))
        setCustomers(c)
        if (c.length) setCustomerId(c[0].id)
      })
      .catch((e) => setError(e.message))
  }, [])

  const filtered = products.filter(
    (p) => p.name.toLowerCase().includes(search.toLowerCase()) || p.sku?.toLowerCase().includes(search.toLowerCase())
  )

  const addToCart = (product: Product) => {
    setCart((prev) => {
      const existing = prev.find((x) => x.product.id === product.id)
      if (existing) {
        if (existing.quantity >= product.stockQuantity) return prev
        return prev.map((x) => x.product.id === product.id ? { ...x, quantity: x.quantity + 1 } : x)
      }
      return [...prev, { product, quantity: 1 }]
    })
  }

  const subTotal = cart.reduce((s, i) => s + i.product.salePrice * i.quantity, 0)
  const discount = discountAmount + subTotal * (discountPercent / 100)
  const total = Math.max(0, subTotal - discount)

  const checkout = async () => {
    if (!cart.length) return
    setLoading(true)
    setError('')
    setMessage('')
    try {
      const sale = await salesApi.create({
        customerId,
        employeeId: user?.employeeId,
        lines: cart.map((i) => ({ productId: i.product.id, quantity: i.quantity })),
        discountAmount,
        discountPercent,
        paymentMode,
        amountPaid: paymentMode === 4 ? 0 : total,
      })
      setMessage(`Vente #${sale.id} enregistrée — Total : ${formatMoney(sale.totalAmount)}`)
      setLastSaleId(sale.id)
      setCart([])
      const p = await productsApi.list()
      setProducts(p.filter((x) => x.stockQuantity > 0))
      // Impression automatique du ticket Mbacke Cosmetique
      try { await openReceiptPrint(sale.id) } catch { /* popup bloquée */ }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erreur')
    } finally {
      setLoading(false)
    }
  }

  const printReceipt = async () => {
    if (!lastSaleId) return
    try {
      await openReceiptPrint(lastSaleId)
    } catch {
      setError('Erreur lors de l\'impression du reçu')
    }
  }

  return (
    <>
      <PageHeader title="Caisse / Vente au comptoir" subtitle="Enregistrez les ventes rapidement" />
      {error && <Alert type="error">{error}</Alert>}
      {message && (
        <Alert type="success">
          {message}
          {lastSaleId && <Btn variant="ghost" onClick={printReceipt} style={{ marginLeft: 12 }}>🖨️ Imprimer reçu</Btn>}
        </Alert>
      )}

      <div className="pos-layout">
        <div className="pos-products">
          <input className="search-input" placeholder="Rechercher un produit..." value={search} onChange={(e) => setSearch(e.target.value)} />
          <div className="product-grid">
            {filtered.map((p) => (
              <button key={p.id} className="product-tile" onClick={() => addToCart(p)}>
                <strong>{p.name}</strong>
                <span>{formatMoney(p.salePrice)}</span>
                <small>Stock: {p.stockQuantity}</small>
              </button>
            ))}
          </div>
        </div>

        <Card className="pos-cart">
          <h3>Panier ({cart.length})</h3>
          {cart.length === 0 ? (
            <p className="muted">Cliquez sur un produit pour l'ajouter</p>
          ) : (
            <ul className="cart-list">
              {cart.map((item) => (
                <li key={item.product.id}>
                  <div>
                    <strong>{item.product.name}</strong>
                    <small>{formatMoney(item.product.salePrice)}</small>
                  </div>
                  <div className="cart-qty">
                    <button onClick={() => setCart((c) => c.map((x) => x.product.id === item.product.id ? { ...x, quantity: Math.max(0, x.quantity - 1) } : x).filter((x) => x.quantity > 0))}>−</button>
                    <span>{item.quantity}</span>
                    <button onClick={() => addToCart(item.product)}>+</button>
                  </div>
                  <strong>{formatMoney(item.product.salePrice * item.quantity)}</strong>
                </li>
              ))}
            </ul>
          )}

          <div className="pos-options">
            <label className="field">
              <span>Client</span>
              <select value={customerId} onChange={(e) => setCustomerId(+e.target.value)}>
                {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </label>
            <label className="field">
              <span>Paiement</span>
              <select value={paymentMode} onChange={(e) => setPaymentMode(+e.target.value)}>
                <option value={1}>Espèces</option>
                <option value={2}>Mobile Money</option>
                <option value={3}>Carte</option>
                <option value={4}>Crédit</option>
              </select>
            </label>
            <div className="field-row">
              <label className="field">
                <span>Remise %</span>
                <input type="number" min={0} max={100} value={discountPercent} onChange={(e) => setDiscountPercent(+e.target.value)} />
              </label>
              <label className="field">
                <span>Remise F</span>
                <input type="number" min={0} value={discountAmount} onChange={(e) => setDiscountAmount(+e.target.value)} />
              </label>
            </div>
          </div>

          <div className="pos-total">
            <div><span>Sous-total</span><strong>{formatMoney(subTotal)}</strong></div>
            <div><span>Remise</span><strong>-{formatMoney(discount)}</strong></div>
            <div className="total-line"><span>TOTAL</span><strong>{formatMoney(total)}</strong></div>
          </div>

          <Btn className="btn-block" disabled={!cart.length || loading} onClick={checkout}>
            {loading ? 'Enregistrement...' : 'Valider la vente'}
          </Btn>
        </Card>
      </div>
    </>
  )
}
