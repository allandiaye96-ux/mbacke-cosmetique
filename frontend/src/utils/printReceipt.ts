/** Ouvre le ticket de caisse format imprimante thermique 80mm (Mbacke Cosmetique) */
export async function openReceiptPrint(saleId: number) {
  const token = localStorage.getItem('token')
  const res = await fetch(`/api/sales/${saleId}/receipt/print`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  if (!res.ok) throw new Error('Impossible d\'imprimer le reçu')
  const html = await res.text()
  const w = window.open('', '_blank', 'width=360,height=700')
  if (!w) {
    alert('Autorisez les fenêtres popup pour imprimer le reçu.')
    return
  }
  w.document.open()
  w.document.write(html)
  w.document.close()
}
