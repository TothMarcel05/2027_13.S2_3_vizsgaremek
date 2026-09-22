import { useState } from 'react'

function App() {
  // Létrehozunk egy memóriát a számnak, ami alapból 0
  const [szamlalo, setSzamlalo] = useState<number>(0)

  return (
    <div style={{ padding: '50px', textAlign: 'center' }}>
      <h1>Kattintás számláló</h1>
      
      {/* Megjelenítjük a memóriadoboz aktuális értékét */}
      <p>Hányszor kattintottál? <strong>{szamlalo}</strong></p>

      {/* Gombnyomásra meghívjuk a setSzamlalo-t, és növeljük az értéket 1-gyel */}
      <button onClick={() => setSzamlalo(szamlalo + 1)}>
        Kattints ide!
      </button>
    </div>
  )
}

export default App