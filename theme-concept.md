import React, { useState } from 'react';

import { Play, Square, RotateCcw, Terminal, HardDrive, Cpu, MemoryStick, Settings, Plus, Search, Trash2, Upload, Download, Copy, ChevronRight, LayoutGrid, List, Filter, SortAsc, Pin, MoreHorizontal, ExternalLink, RefreshCw } from 'lucide-react';



// Concept 3: Windows 11 Fluent Design Inspired (with Mono Font)

export default function WSLManagerConcept3() {

&nbsp; const \[viewMode, setViewMode] = useState('grid');

&nbsp; const \[selectedDistros, setSelectedDistros] = useState(\[]);



&nbsp; const distros = \[

&nbsp;   { id: 1, name: 'Ubuntu 22.04 LTS', icon: '🟠', status: 'running', version: 'WSL 2', size: '15.2 GB', cpu: 12, ram: 2.4, pinned: true },

&nbsp;   { id: 2, name: 'Debian 12', icon: '🔴', status: 'running', version: 'WSL 2', size: '8.7 GB', cpu: 5, ram: 1.1, pinned: true },

&nbsp;   { id: 3, name: 'Arch Linux', icon: '🔵', status: 'stopped', version: 'WSL 2', size: '22.1 GB', cpu: 0, ram: 0, pinned: false },

&nbsp;   { id: 4, name: 'Alpine Linux', icon: '🟢', status: 'running', version: 'WSL 2', size: '1.2 GB', cpu: 2, ram: 0.3, pinned: false },

&nbsp;   { id: 5, name: 'Fedora 39', icon: '🔵', status: 'stopped', version: 'WSL 2', size: '12.8 GB', cpu: 0, ram: 0, pinned: false },

&nbsp;   { id: 6, name: 'openSUSE Leap', icon: '🟢', status: 'stopped', version: 'WSL 2', size: '18.4 GB', cpu: 0, ram: 0, pinned: false },

&nbsp; ];



&nbsp; const runningCount = distros.filter(d => d.status === 'running').length;

&nbsp; const totalRam = distros.reduce((acc, d) => acc + d.ram, 0);

&nbsp; const totalCpu = distros.reduce((acc, d) => acc + d.cpu, 0);



&nbsp; return (

&nbsp;   <div className="min-h-screen text-gray-900 font-mono" style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%)' }}>

&nbsp;     {/\* Mica-style background \*/}

&nbsp;     <div className="min-h-screen backdrop-blur-3xl" style={{ backgroundColor: 'rgba(32, 32, 32, 0.85)' }}>

&nbsp;       <div className="flex h-screen text-white">

&nbsp;         {/\* Navigation Rail \*/}

&nbsp;         <div className="w-12 flex flex-col items-center py-3 gap-1">

&nbsp;           <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-sm mb-4">

&nbsp;             W

&nbsp;           </div>

&nbsp;           <button className="w-10 h-10 rounded-lg bg-white/10 text-white flex items-center justify-center">

&nbsp;             <LayoutGrid className="w-5 h-5" />

&nbsp;           </button>

&nbsp;           <button className="w-10 h-10 rounded-lg hover:bg-white/5 text-gray-400 flex items-center justify-center transition-colors">

&nbsp;             <Terminal className="w-5 h-5" />

&nbsp;           </button>

&nbsp;           <button className="w-10 h-10 rounded-lg hover:bg-white/5 text-gray-400 flex items-center justify-center transition-colors">

&nbsp;             <Download className="w-5 h-5" />

&nbsp;           </button>

&nbsp;           <div className="flex-1" />

&nbsp;           <button className="w-10 h-10 rounded-lg hover:bg-white/5 text-gray-400 flex items-center justify-center transition-colors">

&nbsp;             <Settings className="w-5 h-5" />

&nbsp;           </button>

&nbsp;         </div>



&nbsp;         {/\* Main Content \*/}

&nbsp;         <div className="flex-1 flex flex-col bg-\[#202020]/60 rounded-l-lg overflow-hidden border-l border-t border-white/10">

&nbsp;           {/\* Title Bar \*/}

&nbsp;           <div className="h-10 flex items-center px-4 border-b border-white/5">

&nbsp;             <span className="text-sm text-gray-400">WSL Manager</span>

&nbsp;             <div className="flex-1 flex justify-center">

&nbsp;               <div className="flex gap-2">

&nbsp;                 <div className="w-3 h-3 rounded-full bg-red-500/80 hover:bg-red-500 cursor-pointer" />

&nbsp;                 <div className="w-3 h-3 rounded-full bg-yellow-500/80 hover:bg-yellow-500 cursor-pointer" />

&nbsp;                 <div className="w-3 h-3 rounded-full bg-green-500/80 hover:bg-green-500 cursor-pointer" />

&nbsp;               </div>

&nbsp;             </div>

&nbsp;             <div className="w-20" />

&nbsp;           </div>



&nbsp;           {/\* Header \*/}

&nbsp;           <div className="p-6 pb-4">

&nbsp;             <div className="flex items-center justify-between mb-6">

&nbsp;               <div>

&nbsp;                 <h1 className="text-2xl font-semibold mb-1">Distributions</h1>

&nbsp;                 <p className="text-sm text-gray-400">{runningCount} running • {distros.length} total</p>

&nbsp;               </div>

&nbsp;               <button className="px-4 py-2 bg-\[#0078d4] hover:bg-\[#1084d8] rounded-md flex items-center gap-2 text-sm font-medium transition-colors">

&nbsp;                 <Plus className="w-4 h-4" />

&nbsp;                 Install new

&nbsp;               </button>

&nbsp;             </div>



&nbsp;             {/\* Stats Bar \*/}

&nbsp;             <div className="flex gap-6 mb-6">

&nbsp;               <div className="flex items-center gap-3">

&nbsp;                 <div className="w-10 h-10 rounded-lg bg-cyan-500/20 flex items-center justify-center">

&nbsp;                   <Cpu className="w-5 h-5 text-cyan-400" />

&nbsp;                 </div>

&nbsp;                 <div>

&nbsp;                   <div className="text-lg font-semibold">{totalCpu}%</div>

&nbsp;                   <div className="text-xs text-gray-400">CPU Usage</div>

&nbsp;                 </div>

&nbsp;               </div>

&nbsp;               <div className="flex items-center gap-3">

&nbsp;                 <div className="w-10 h-10 rounded-lg bg-purple-500/20 flex items-center justify-center">

&nbsp;                   <MemoryStick className="w-5 h-5 text-purple-400" />

&nbsp;                 </div>

&nbsp;                 <div>

&nbsp;                   <div className="text-lg font-semibold">{totalRam.toFixed(1)} GB</div>

&nbsp;                   <div className="text-xs text-gray-400">Memory</div>

&nbsp;                 </div>

&nbsp;               </div>

&nbsp;               <div className="flex items-center gap-3">

&nbsp;                 <div className="w-10 h-10 rounded-lg bg-amber-500/20 flex items-center justify-center">

&nbsp;                   <HardDrive className="w-5 h-5 text-amber-400" />

&nbsp;                 </div>

&nbsp;                 <div>

&nbsp;                   <div className="text-lg font-semibold">88.4 GB</div>

&nbsp;                   <div className="text-xs text-gray-400">Disk Used</div>

&nbsp;                 </div>

&nbsp;               </div>

&nbsp;             </div>



&nbsp;             {/\* Toolbar \*/}

&nbsp;             <div className="flex items-center gap-3">

&nbsp;               <div className="relative flex-1 max-w-md">

&nbsp;                 <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />

&nbsp;                 <input

&nbsp;                   type="text"

&nbsp;                   placeholder="Search distributions"

&nbsp;                   className="w-full bg-white/5 border border-white/10 rounded-md pl-10 pr-4 py-2 text-sm placeholder-gray-500 focus:outline-none focus:border-\[#0078d4] focus:ring-1 focus:ring-\[#0078d4] transition-all"

&nbsp;                 />

&nbsp;               </div>

&nbsp;               <button className="p-2 hover:bg-white/5 rounded-md transition-colors">

&nbsp;                 <Filter className="w-4 h-4 text-gray-400" />

&nbsp;               </button>

&nbsp;               <button className="p-2 hover:bg-white/5 rounded-md transition-colors">

&nbsp;                 <SortAsc className="w-4 h-4 text-gray-400" />

&nbsp;               </button>

&nbsp;               <div className="w-px h-6 bg-white/10" />

&nbsp;               <button 

&nbsp;                 onClick={() => setViewMode('grid')}

&nbsp;                 className={`p-2 rounded-md transition-colors ${viewMode === 'grid' ? 'bg-white/10' : 'hover:bg-white/5'}`}

&nbsp;               >

&nbsp;                 <LayoutGrid className="w-4 h-4 text-gray-400" />

&nbsp;               </button>

&nbsp;               <button 

&nbsp;                 onClick={() => setViewMode('list')}

&nbsp;                 className={`p-2 rounded-md transition-colors ${viewMode === 'list' ? 'bg-white/10' : 'hover:bg-white/5'}`}

&nbsp;               >

&nbsp;                 <List className="w-4 h-4 text-gray-400" />

&nbsp;               </button>

&nbsp;               <div className="w-px h-6 bg-white/10" />

&nbsp;               <button className="p-2 hover:bg-white/5 rounded-md transition-colors">

&nbsp;                 <RefreshCw className="w-4 h-4 text-gray-400" />

&nbsp;               </button>

&nbsp;             </div>

&nbsp;           </div>



&nbsp;           {/\* Content \*/}

&nbsp;           <div className="flex-1 overflow-auto px-6 pb-6">

&nbsp;             {/\* Pinned Section \*/}

&nbsp;             {distros.some(d => d.pinned) \&\& (

&nbsp;               <div className="mb-6">

&nbsp;                 <h2 className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-3 flex items-center gap-2">

&nbsp;                   <Pin className="w-3 h-3" />

&nbsp;                   Pinned

&nbsp;                 </h2>

&nbsp;                 <div className="grid grid-cols-2 gap-3">

&nbsp;                   {distros.filter(d => d.pinned).map(distro => (

&nbsp;                     <div

&nbsp;                       key={distro.id}

&nbsp;                       className="group bg-white/5 hover:bg-white/\[0.08] border border-white/10 hover:border-white/20 rounded-lg p-4 transition-all cursor-pointer"

&nbsp;                     >

&nbsp;                       <div className="flex items-start justify-between mb-3">

&nbsp;                         <div className="flex items-center gap-3">

&nbsp;                           <span className="text-2xl">{distro.icon}</span>

&nbsp;                           <div>

&nbsp;                             <div className="font-medium">{distro.name}</div>

&nbsp;                             <div className="text-xs text-gray-400">{distro.version} • {distro.size}</div>

&nbsp;                           </div>

&nbsp;                         </div>

&nbsp;                         <div className={`px-2 py-0.5 rounded text-xs ${

&nbsp;                           distro.status === 'running' 

&nbsp;                             ? 'bg-green-500/20 text-green-400' 

&nbsp;                             : 'bg-gray-500/20 text-gray-400'

&nbsp;                         }`}>

&nbsp;                           {distro.status}

&nbsp;                         </div>

&nbsp;                       </div>

&nbsp;                       

&nbsp;                       {distro.status === 'running' \&\& (

&nbsp;                         <div className="flex gap-4 mb-3 text-xs">

&nbsp;                           <span className="text-cyan-400">CPU {distro.cpu}%</span>

&nbsp;                           <span className="text-purple-400">RAM {distro.ram} GB</span>

&nbsp;                         </div>

&nbsp;                       )}



&nbsp;                       <div className="flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">

&nbsp;                         {distro.status === 'running' ? (

&nbsp;                           <>

&nbsp;                             <button className="flex-1 py-1.5 bg-\[#0078d4] hover:bg-\[#1084d8] rounded text-xs font-medium transition-colors flex items-center justify-center gap-1">

&nbsp;                               <Terminal className="w-3 h-3" />

&nbsp;                               Terminal

&nbsp;                             </button>

&nbsp;                             <button className="px-3 py-1.5 bg-white/5 hover:bg-white/10 rounded text-xs transition-colors">

&nbsp;                               <Square className="w-3 h-3" />

&nbsp;                             </button>

&nbsp;                           </>

&nbsp;                         ) : (

&nbsp;                           <button className="flex-1 py-1.5 bg-green-600 hover:bg-green-500 rounded text-xs font-medium transition-colors flex items-center justify-center gap-1">

&nbsp;                             <Play className="w-3 h-3" />

&nbsp;                             Start

&nbsp;                           </button>

&nbsp;                         )}

&nbsp;                         <button className="px-3 py-1.5 bg-white/5 hover:bg-white/10 rounded text-xs transition-colors">

&nbsp;                           <MoreHorizontal className="w-3 h-3" />

&nbsp;                         </button>

&nbsp;                       </div>

&nbsp;                     </div>

&nbsp;                   ))}

&nbsp;                 </div>

&nbsp;               </div>

&nbsp;             )}



&nbsp;             {/\* All Distributions \*/}

&nbsp;             <div>

&nbsp;               <h2 className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-3">All Distributions</h2>

&nbsp;               

&nbsp;               {viewMode === 'grid' ? (

&nbsp;                 <div className="grid grid-cols-3 gap-3">

&nbsp;                   {distros.filter(d => !d.pinned).map(distro => (

&nbsp;                     <div

&nbsp;                       key={distro.id}

&nbsp;                       className="group bg-white/5 hover:bg-white/\[0.08] border border-white/10 hover:border-white/20 rounded-lg p-4 transition-all cursor-pointer"

&nbsp;                     >

&nbsp;                       <div className="flex items-center gap-3 mb-3">

&nbsp;                         <span className="text-xl">{distro.icon}</span>

&nbsp;                         <div className="flex-1 min-w-0">

&nbsp;                           <div className="font-medium truncate">{distro.name}</div>

&nbsp;                           <div className="text-xs text-gray-400">{distro.size}</div>

&nbsp;                         </div>

&nbsp;                         <div className={`w-2 h-2 rounded-full ${

&nbsp;                           distro.status === 'running' ? 'bg-green-400' : 'bg-gray-600'

&nbsp;                         }`} />

&nbsp;                       </div>

&nbsp;                       

&nbsp;                       <div className="flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">

&nbsp;                         {distro.status === 'running' ? (

&nbsp;                           <>

&nbsp;                             <button className="flex-1 py-1.5 bg-\[#0078d4] hover:bg-\[#1084d8] rounded text-xs transition-colors">

&nbsp;                               Open

&nbsp;                             </button>

&nbsp;                             <button className="px-2 py-1.5 bg-white/5 hover:bg-white/10 rounded text-xs transition-colors">

&nbsp;                               <Square className="w-3 h-3" />

&nbsp;                             </button>

&nbsp;                           </>

&nbsp;                         ) : (

&nbsp;                           <button className="flex-1 py-1.5 bg-white/5 hover:bg-white/10 rounded text-xs transition-colors flex items-center justify-center gap-1">

&nbsp;                             <Play className="w-3 h-3" />

&nbsp;                             Start

&nbsp;                           </button>

&nbsp;                         )}

&nbsp;                       </div>

&nbsp;                     </div>

&nbsp;                   ))}

&nbsp;                   

&nbsp;                   {/\* Add New Card \*/}

&nbsp;                   <button className="border-2 border-dashed border-white/10 hover:border-white/20 rounded-lg p-4 flex flex-col items-center justify-center gap-2 text-gray-500 hover:text-gray-400 transition-colors min-h-\[120px]">

&nbsp;                     <Plus className="w-6 h-6" />

&nbsp;                     <span className="text-xs">Add distribution</span>

&nbsp;                   </button>

&nbsp;                 </div>

&nbsp;               ) : (

&nbsp;                 <div className="bg-white/5 rounded-lg border border-white/10 overflow-hidden">

&nbsp;                   <table className="w-full text-sm">

&nbsp;                     <thead>

&nbsp;                       <tr className="border-b border-white/10 text-xs text-gray-400 uppercase tracking-wider">

&nbsp;                         <th className="text-left p-3 font-medium">Name</th>

&nbsp;                         <th className="text-left p-3 font-medium">Status</th>

&nbsp;                         <th className="text-left p-3 font-medium">Version</th>

&nbsp;                         <th className="text-left p-3 font-medium">Size</th>

&nbsp;                         <th className="text-right p-3 font-medium">Actions</th>

&nbsp;                       </tr>

&nbsp;                     </thead>

&nbsp;                     <tbody>

&nbsp;                       {distros.map(distro => (

&nbsp;                         <tr key={distro.id} className="border-b border-white/5 hover:bg-white/\[0.02] transition-colors">

&nbsp;                           <td className="p-3">

&nbsp;                             <div className="flex items-center gap-3">

&nbsp;                               <span>{distro.icon}</span>

&nbsp;                               <span>{distro.name}</span>

&nbsp;                             </div>

&nbsp;                           </td>

&nbsp;                           <td className="p-3">

&nbsp;                             <span className={`inline-flex items-center gap-1.5 ${

&nbsp;                               distro.status === 'running' ? 'text-green-400' : 'text-gray-500'

&nbsp;                             }`}>

&nbsp;                               <span className={`w-1.5 h-1.5 rounded-full ${

&nbsp;                                 distro.status === 'running' ? 'bg-green-400' : 'bg-gray-600'

&nbsp;                               }`} />

&nbsp;                               {distro.status}

&nbsp;                             </span>

&nbsp;                           </td>

&nbsp;                           <td className="p-3 text-gray-400">{distro.version}</td>

&nbsp;                           <td className="p-3 text-gray-400">{distro.size}</td>

&nbsp;                           <td className="p-3 text-right">

&nbsp;                             <button className="px-3 py-1 bg-\[#0078d4] hover:bg-\[#1084d8] rounded text-xs transition-colors">

&nbsp;                               Open

&nbsp;                             </button>

&nbsp;                           </td>

&nbsp;                         </tr>

&nbsp;                       ))}

&nbsp;                     </tbody>

&nbsp;                   </table>

&nbsp;                 </div>

&nbsp;               )}

&nbsp;             </div>

&nbsp;           </div>

&nbsp;         </div>

&nbsp;       </div>

&nbsp;     </div>

&nbsp;   </div>

&nbsp; );

}

