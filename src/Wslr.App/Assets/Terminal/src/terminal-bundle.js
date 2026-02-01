// Terminal bundle entry point
// Exports xterm.js and addons to global scope for terminal.html

import { Terminal } from '@xterm/xterm';
import { FitAddon } from '@xterm/addon-fit';

// Export to global scope for use in terminal.html
window.Terminal = Terminal;
window.FitAddon = { FitAddon };
