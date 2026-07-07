import React from "react";
import { createRoot } from "react-dom/client";
import SupportCatalogView from "./views/SupportCatalogView";
import "./catalog.css";

const root = document.getElementById("catalog-root");
if (!root) {
  throw new Error("catalog-root element not found");
}

createRoot(root).render(
  <React.StrictMode>
    <SupportCatalogView />
  </React.StrictMode>
);
