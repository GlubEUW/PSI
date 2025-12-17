import { useState } from "react";
import { DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem } from "pixel-retroui";
import "./Dropdown.css";

const Dropdown = ({ bg, options, onSelect }) => {
   const [selected, setSelected] = useState(options[0]);
   
   const handleSelect = (value) => {
      setSelected(value);
      if (onSelect) onSelect(value);
   };

   return (
      <DropdownMenu value={selected} onValueChange={setSelected}>
         <DropdownMenuTrigger
            bg={bg}
            style={{ "--bg": bg }}
            borderColor="#fff"
            className="dropdown w-20 h-5"
         >
            {selected}
         </DropdownMenuTrigger>
         <DropdownMenuContent 
            bg={bg}
            style={{ "--bg": bg }}
            borderColor="#fff"
            className="dropdown-min-width w-20"
         >
            {options.map((option, index) => (
               <DropdownMenuItem key={index} className="dropdown" value={option}>
                  <button 
                     className="w-full" 
                     style={{ background: "transparent", border: "none", padding: 0, margin: 0 }}
                     onClick={() => handleSelect(option)}>
                     {option}
                  </button>
               </DropdownMenuItem>
            ))}
         </DropdownMenuContent>
      </DropdownMenu>
   );
}

export default Dropdown;