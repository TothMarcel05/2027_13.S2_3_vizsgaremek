const express = require('express');
const cors = require('cors');

const app = express();
const PORT = 5000;

// Engedélyezzük, hogy a frontendünk elérhesse a szervert
app.use(cors());
// Engedélyezzük, hogy a szerver fogadni tudjon JSON adatokat
app.use(express.json());

// Ez a teszt végpontunk (route)
app.get('/api/status', (req, res) => {
    res.json({ message: "Szerver fut és válaszol!" });
});

// Elindítjuk a szervert a megadott porton
app.listen(PORT, () => {
    console.log(`A backend szerver elindult a http://localhost:${PORT} címen`);
});