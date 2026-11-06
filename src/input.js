// Entrada de teclado e mouse.

export class Input {
  constructor(canvas) {
    this.keys = new Set();
    this.pressed = new Set(); // teclas apertadas neste frame
    this.mouse = { x: innerWidth / 2, y: innerHeight / 2, left: false, right: false, rightPressed: false, wheel: 0 };
    this.canvas = canvas;

    addEventListener('keydown', (e) => {
      const k = e.key.toLowerCase();
      if (!this.keys.has(k)) this.pressed.add(k);
      this.keys.add(k);
      if ([' ', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright', 'tab'].includes(k)) e.preventDefault();
    });
    addEventListener('keyup', (e) => this.keys.delete(e.key.toLowerCase()));
    addEventListener('blur', () => { this.keys.clear(); this.mouse.left = this.mouse.right = false; });

    addEventListener('mousemove', (e) => { this.mouse.x = e.clientX; this.mouse.y = e.clientY; });
    addEventListener('mousedown', (e) => {
      if (e.button === 0) this.mouse.left = true;
      if (e.button === 2) { this.mouse.right = true; this.mouse.rightPressed = true; }
    });
    addEventListener('mouseup', (e) => {
      if (e.button === 0) this.mouse.left = false;
      if (e.button === 2) this.mouse.right = false;
    });
    addEventListener('contextmenu', (e) => e.preventDefault());
    addEventListener('wheel', (e) => { this.mouse.wheel += Math.sign(e.deltaY); }, { passive: true });
  }

  down(k) { return this.keys.has(k); }
  hit(k) { return this.pressed.has(k); }

  endFrame() {
    this.pressed.clear();
    this.mouse.rightPressed = false;
    this.mouse.wheel = 0;
  }
}
