import { useEffect, useState } from "react";

function App() {
  const [data, setData] = useState(null);

useEffect(() => {
  fetch("http://localhost:5243/WeatherForecast")
    .then((res) => res.json())
    .then((result) => setData(result))
    .catch((err) => console.error("Fetch error:", err));
}, []);

  return (
    <div>
      <h1>React + .NET</h1>
      {data ? (
        <ul>
          {data.map((item, idx) => (
            <li key={idx}>{item.summary} - {item.temperatureC}°C</li>
          ))}
        </ul>
      ) : (
        <p>Loading...</p>
      )}
    </div>
  );
}

export default App;
