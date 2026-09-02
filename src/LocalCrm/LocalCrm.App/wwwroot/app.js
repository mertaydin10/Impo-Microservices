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
  }
});
