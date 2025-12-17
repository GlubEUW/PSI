export async function GetLeaderBoard(page, pageSize) {
    const url = new URL("http://localhost:5243/api/LeaderBoard");
    url.searchParams.set("page", page);
    url.searchParams.set("pageSize", pageSize);

    const response = await fetch(url.toString(), {
        method: "GET",
        headers: {
            "Content-Type": "application/json"
        }
    });

    return response;
}
