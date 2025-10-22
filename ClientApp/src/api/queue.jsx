export async function CanJoinQueue(token) {
   const response = await fetch("http://localhost:5243/api/queue", {
      method: "POST",
      headers: {
         "Content-Type": "application/json",
         "Authorization": "Bearer " + token
      },
   });
   return response;
}