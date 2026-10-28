// Shared page behaviour: back-button protection, fading messages, Yes / No confirmations and live search.
(() => {
  // ---- Forms marked data-confirm ask Yes / No in a dialog before they submit ----
  // Listens on the document, so rows added later by the live search are covered too.
  let confirmDialog = null;
  let pendingForm = null;

  const openConfirm = form => {
    if (!confirmDialog) {
      confirmDialog = document.createElement('dialog');
      confirmDialog.className = 'confirm-dialog';
      confirmDialog.innerHTML = `
        <p class="confirm-title"></p>
        <p class="confirm-detail"></p>
        <div class="confirm-buttons">
          <button type="button" class="btn btn-light" data-answer="no">No</button>
          <button type="button" class="btn" data-answer="yes"></button>
        </div>`;
      document.body.append(confirmDialog);
      confirmDialog.querySelector('[data-answer="no"]').addEventListener('click', () => confirmDialog.close());
      // Clicking the dimmed background also counts as No.
      confirmDialog.addEventListener('click', event => {
        if (event.target === confirmDialog) {
          confirmDialog.close();
        }
      });
      // form.submit() skips the submit event, so the dialog does not open again.
      confirmDialog.querySelector('[data-answer="yes"]').addEventListener('click', () => {
        confirmDialog.close();
        pendingForm?.submit();
      });
    }

    pendingForm = form;
    const yes = confirmDialog.querySelector('[data-answer="yes"]');
    confirmDialog.querySelector('.confirm-title').textContent = form.dataset.confirm;
    confirmDialog.querySelector('.confirm-detail').textContent = form.dataset.confirmDetail ?? '';
    yes.textContent = form.dataset.confirmYes ?? 'Yes';
    yes.className = `btn ${form.dataset.confirmTone === 'success' ? 'btn-primary' : 'btn-danger'}`;
    confirmDialog.showModal();
    confirmDialog.querySelector('[data-answer="no"]').focus();
  };

  document.addEventListener('submit', event => {
    if (event.target.matches('form[data-confirm]')) {
      event.preventDefault();
      openConfirm(event.target);
    }
  });

  // ---- Live search: the results update while typing, without reloading the page ----
  // data-live-search names the element to swap; the server renders the results as usual.
  document.querySelectorAll('[data-live-search]').forEach(input => {
    const form = input.form;
    const selector = input.dataset.liveSearch;
    let timer = null;
    let request = null;

    input.addEventListener('input', () => {
      clearTimeout(timer);
      timer = setTimeout(async () => {
        const url = `${form.action}?${new URLSearchParams(new FormData(form))}`;
        request?.abort();
        request = new AbortController();
        try {
          const response = await fetch(url, { signal: request.signal });
          // Signed out in the meantime: follow the redirect to the login page.
          if (response.redirected) {
            window.location.href = response.url;
            return;
          }
          const page = new DOMParser().parseFromString(await response.text(), 'text/html');
          document.querySelector(selector).replaceWith(page.querySelector(selector));
          const count = form.querySelector('.toolbar-count');
          if (count) {
            count.textContent = page.querySelector('.toolbar-count')?.textContent ?? '';
          }
          history.replaceState(null, '', url);
        } catch (error) {
          if (error.name !== 'AbortError') {
            form.submit();
          }
        }
      }, 250);
    });
  });

  // ---- A page restored from the back / forward cache is reloaded, so the server re-checks the login ----
  window.addEventListener('pageshow', event => {
    if (event.persisted) {
      window.location.reload();
    }
  });

  // ---- Success messages fade out after a few seconds; any message can be closed ----
  document.querySelectorAll('.alert').forEach(alert => {
    const hide = () => {
      alert.classList.add('is-hiding');
      setTimeout(() => alert.remove(), 400);
    };
    alert.querySelector('.alert-close')?.addEventListener('click', hide);
    if (alert.dataset.autodismiss) {
      setTimeout(hide, Number(alert.dataset.autodismiss));
    }
  });
})();
