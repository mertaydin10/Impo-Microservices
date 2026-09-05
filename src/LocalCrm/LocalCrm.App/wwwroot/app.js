window.impoDownload = (name, text) => {
  const blob = new Blob([text], { type: "text/csv;charset=utf-8" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = name;
  link.click();
  URL.revokeObjectURL(link.href);
};

document.addEventListener("keydown", event => {
  if ((event.metaKey || event.ctrlKey) && event.key === "s") {
    event.preventDefault();
    document.querySelector(".save")?.click();
    return;
  }

  const tag = (event.target.tagName || "").toLowerCase();
  const typing = tag === "input" || tag === "textarea" || tag === "select" || event.target.isContentEditable;
  if (typing) return;

  if (event.key === "n" || event.key === "N") {
    event.preventDefault();
    const buttons = [...document.querySelectorAll(".toolbar button")];
    buttons.find(button => button.textContent.includes("Yeni kişi"))?.click();
  } else if (event.key === "/") {
    event.preventDefault();
    document.getElementById("crmSearch")?.focus();
  }
});
