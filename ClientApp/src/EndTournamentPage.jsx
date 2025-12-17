import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Card } from "pixel-retroui";
import RetroButton from "./components/RetroButton";

function EndTournamentPage() {
    const navigate = useNavigate();
    const { state } = useLocation();

    const players = state?.players ?? [];

    useEffect(() => {
        document.title = "Tournament Results";
    }, []);

    if (!players.length) {
        return (
            <div style={{ textAlign: "center", marginTop: "80px" }}>
                <p>No tournament data available.</p>
                <RetroButton onClick={() => navigate("/home")} w={300} h={40}>
                    Back to Home
                </RetroButton>
            </div>
        );
    }

    const podium = players.slice(0, 3);

    return (
        <div
            style={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                fontSize: "20px",
                marginTop: "40px"
            }}
        >
            <Card
                bg="#ffd700"
                textColor="black"
                borderColor="#fff"
                shadowColor="#000"
                style={{ fontSize: "36px", marginBottom: "32px", padding: "12px 24px" }}
            >
                Tournament Results
            </Card>

            {/* PODIUM */}
            <div style={{ display: "flex", gap: "24px", alignItems: "flex-end" }}>
                {podium[1] && (
                    <PodiumCard place={2} player={podium[1]} height={140} color="#c0c0c0" />
                )}
                {podium[0] && (
                    <PodiumCard place={1} player={podium[0]} height={180} color="#ffd700" />
                )}
                {podium[2] && (
                    <PodiumCard place={3} player={podium[2]} height={120} color="#cd7f32" />
                )}
            </div>

            <hr style={{ width: "60%", margin: "32px 0" }} />

            {/* RANKINGS */}
            <h3>Final Rankings</h3>
            <ol style={{ width: "300px" }}>
                {players.map((p, idx) => (
                    <li key={idx} style={{ marginBottom: "8px" }}>
                        <strong>{p.name}</strong> — {p.wins} wins
                    </li>
                ))}
            </ol>

            <RetroButton
                onClick={() => navigate("/home")}
                w={300}
                h={40}
                style={{
                    marginTop: "32px",
                    position: "relative",
                    zIndex: 10,
                    backgroundColor: "#4a90e2",
                    opacity: 1
                }}
            >
                Back to Home
            </RetroButton>

        </div >
    );
}

function PodiumCard({ place, player, height, color }) {
    return (
        <div style={{ textAlign: "center" }}>
            <Card
                bg={color}
                textColor="black"
                borderColor="#fff"
                shadowColor="#000"
                style={{
                    height,
                    width: "140px",
                    display: "flex",
                    flexDirection: "column",
                    justifyContent: "center",
                    fontSize: "18px"
                }}
            >
                <strong>{place}.</strong>
                <div>{player.name}</div>
                <small>{player.wins} wins</small>
            </Card>
        </div>
    );
}

export default EndTournamentPage;
