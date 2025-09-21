import { useEffect, useState } from "react";

function App() {
  const [data, setData] = useState(null);

  useEffect(() => {
    fetch("https://localhost:5001/WeatherForecast") // .NET endpoint
      .then((res) => res.json())
      .then((result) => setData(result));
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
