import { useState, useEffect } from 'react';
import { Popup, Input, Button } from 'pixel-retroui';

const GuestPopup = ({ handleSubmit, isPopupOpen, closePopup}) => {
   const [username, setUsername] = useState("");
   
   useEffect(() => {
      if (!isPopupOpen) {
         setUsername("");
      }
   }, [isPopupOpen]);

   const onSubmit = (e) => {
      e.preventDefault();
      handleSubmit(username);
   };

   return (
      <Popup
         bg="hsla(358, 90%, 50%, 1.00)"
         baseBg="hsla(358, 90%, 40%, 1.00)"
         isOpen={isPopupOpen}
         onClose={closePopup}
         className="text-center"
      >
         <h1 className="text-3xl mb-4" style={{ color: "#3df5efff" }}>Welcome!</h1>
         <p className="mb-4" style={{ color: "#3df5efff" }}>Please enter a username to continue.</p>

         <form onSubmit={onSubmit} className=" flex flex-col gap-4 items-center">
            <Input 
               bg="#f2f2f2" 
               type="text"
               placeholder="Username" 
               value={username}
               onChange={(e) => setUsername(e.target.value)}
               required
            />

            <Button bg="#3df5efff" type="submit" className="w-20">
               Login
            </Button>
         </form>
      </Popup>
   );
}

export default GuestPopup;