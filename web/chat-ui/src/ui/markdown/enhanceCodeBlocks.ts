const ENHANCED = 'data-dc-code-enhanced'

export function enhanceCodeBlocks(root: HTMLElement): void {
  root.querySelectorAll('pre').forEach((pre) => {
    if (pre.closest(`.dc-code-block`) || pre.hasAttribute(ENHANCED)) return
    pre.setAttribute(ENHANCED, '1')

    const lang =
      pre.querySelector('code')?.className.match(/language-([\w#+-]+)/)?.[1] ??
      pre.getAttribute('data-lang') ??
      ''

    const wrap = document.createElement('div')
    wrap.className = 'dc-code-block'
    const parent = pre.parentNode
    if (!parent) return
    parent.insertBefore(wrap, pre)

    const head = document.createElement('div')
    head.className = 'dc-code-head'
    if (lang) {
      const badge = document.createElement('span')
      badge.className = 'dc-code-lang'
      badge.textContent = lang
      head.appendChild(badge)
    } else {
      head.appendChild(document.createElement('span'))
    }

    const btn = document.createElement('button')
    btn.type = 'button'
    btn.className = 'dc-code-copy'
    btn.textContent = '复制'
    btn.addEventListener('click', async () => {
      const code = pre.querySelector('code')?.textContent ?? pre.textContent ?? ''
      try {
        await navigator.clipboard.writeText(code)
        btn.textContent = '已复制'
      } catch {
        btn.textContent = '复制失败'
      }
      window.setTimeout(() => {
        btn.textContent = '复制'
      }, 2000)
    })
    head.appendChild(btn)
    wrap.appendChild(head)
    wrap.appendChild(pre)
  })
}
