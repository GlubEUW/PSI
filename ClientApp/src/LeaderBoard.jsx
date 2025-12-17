import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import RetroButton from "./components/RetroButton";
import { GetLeaderBoard } from "./api/leaderboard";

function LeaderBoard() {
    const navigate = useNavigate();
    const pageSize = 100;

    const [page, setPage] = useState(0);
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const users = data?.users ?? [];
    const totalUsers = data?.totalUsers ?? 0;

    const canPrev = page > 0;
    const canNext = (page + 1) * pageSize < totalUsers;

    const rangeLabel = useMemo(() => {
        const start = page * pageSize;
        const end = Math.min(start + pageSize, totalUsers);
        return `${start}-${end}`;
    }, [page, pageSize, totalUsers]);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            setLoading(true);
            setError(null);

            try {
                const response = await GetLeaderBoard(page, pageSize);
                if (!response.ok) {
                    setError(`Failed to load leaderboard: Status ${response.status}`);
                    setData(null);
                    return;
                }
                const json = await response.json();
                if (!cancelled) {
                    setData(json);
                }
            } catch (e) {
                if (!cancelled) {
                    setError("Failed to load leaderboard");
                    setData(null);
                }
            } finally {
                if (!cancelled) {
                    setLoading(false);
                }
            }
        }

        load();

        return () => {
            cancelled = true;
        };
    }, [page]);

    return (
        <div style={{ fontSize: "18px" }}>
            <h1>Leaderboard</h1>

            <div style={{ marginBlock: "16px", display: "flex", gap: "12px", flexWrap: "wrap" }}>
                <RetroButton onClick={() => navigate("/home")} bg="#ff9d00ff" w={200} h={40}>Back</RetroButton>
                <RetroButton onClick={() => setPage(p => Math.max(0, p - 1))} disabled={!canPrev} bg="#2aaac4ff" w={200} h={40}>Prev</RetroButton>
                <RetroButton onClick={() => setPage(p => p + 1)} disabled={!canNext} bg="#44b17bff" w={200} h={40}>Next</RetroButton>
            </div>

            <div style={{ marginBottom: "12px" }}>
                Showing {rangeLabel} of {totalUsers}
            </div>

            {loading && <div>Loading...</div>}
            {!loading && error && <div>{error}</div>}

            {!loading && !error && (
                <div style={{ overflowX: "auto" }}>
                    <table style={{ width: "100%", borderCollapse: "collapse" }}>
                        <thead>
                            <tr>
                                <th style={{ textAlign: "left", padding: "8px" }}>#</th>
                                <th style={{ textAlign: "left", padding: "8px" }}>Name</th>
                                <th style={{ textAlign: "left", padding: "8px" }}>Total Wins</th>
                                <th style={{ textAlign: "left", padding: "8px" }}>TicTacToe</th>
                                <th style={{ textAlign: "left", padding: "8px" }}>RockPaperScissors</th>
                                <th style={{ textAlign: "left", padding: "8px" }}>ConnectFour</th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map((u, idx) => (
                                <tr key={u.id ?? `${page}-${idx}`}>
                                    <td style={{ padding: "8px" }}>{page * pageSize + idx + 1}</td>
                                    <td style={{ padding: "8px" }}>{u.name}</td>
                                    <td style={{ padding: "8px" }}>{u.totalWins}</td>
                                    <td style={{ padding: "8px" }}>{u.ticTacToeWins}</td>
                                    <td style={{ padding: "8px" }}>{u.rockPaperScissorsWins}</td>
                                    <td style={{ padding: "8px" }}>{u.connectFourWins}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}

export default LeaderBoard;
