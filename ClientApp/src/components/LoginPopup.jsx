import { useState, useEffect } from 'react';
import { Popup, Input, Button } from 'pixel-retroui';

const LoginPopup = ({ handleSubmit, isPopupOpen, closePopup}) => {
   const [username, setUsername] = useState("");
   const [password, setPassword] = useState("");

   useEffect(() => {
      if (!isPopupOpen) {
         setUsername("");
         setPassword("");
      }
   }, [isPopupOpen]);

   const onSubmit = (e) => {
      e.preventDefault();
      handleSubmit(username, password);
   };

   return (
      <Popup
         bg="#cccc00ff"
         baseBg="hsla(60, 100%, 30%, 1.00)"
         isOpen={isPopupOpen}
         onClose={closePopup}
         className="text-center"
      >
         <h1 className="text-3xl mb-4" style={{ color: "#0000ffff" }}>Welcome!</h1>
         <p className="mb-4" style={{ color: "#0000ffff" }}>Please login to continue.</p>

         <form onSubmit={onSubmit} className=" flex flex-col gap-4 items-center">
            <Input 
               bg="#f2f2f2" 
               type="text"
               placeholder="Username" 
               value={username}
               onChange={(e) => setUsername(e.target.value)}
               required
               autoComplete="username"
            />
            <Input
               bg="#f2f2f2"
               type="password"
               placeholder="Password"
               value={password}
               onChange={(e) => setPassword(e.target.value)}
               required
               autoComplete="current-password"
            />

            <Button bg="#0000ffff" type="submit" className="w-20" textColor="#fff">
               Login
            </Button>
         </form>
      </Popup>
   );
}

export default LoginPopup;