import { GlobalWorkerOptions } from '../pdfjs-dist/dist/build/pdf.min.mjs';
import { EventBus, PDFLinkService, PDFFindController, PDFViewer } from '../pdfjs-dist/dist/web/pdf_viewer.mjs';

GlobalWorkerOptions.workerSrc = '../pdfjs-dist/dist/build/pdf.worker.min.mjs';

// Extract the file path from the URL query string.
const url = new URL(window.location);
const fileUrl = url.searchParams.get('file');
if (!fileUrl) {
  throw new Error('File not specified in the URL query string');
}

const container = document.getElementById('viewerContainer');
const eventBus = new EventBus();

// Enable hyperlinks within PDF files.
const pdfLinkService = new PDFLinkService({
  eventBus,
});

// Enable the find controller.
const pdfFindController = new PDFFindController({
  eventBus,
  linkService: pdfLinkService,
});

// Create the PDF viewer.
const pdfViewer = new PDFViewer({
  container,
  eventBus,
  linkService: pdfLinkService,
  findController: pdfFindController,
});
pdfLinkService.setViewer(pdfViewer);

// Establish the page geometry before applying a page citation.
eventBus.on('pagesinit', function () {
  pdfViewer.currentScaleValue = 'page-width';
});
function navigateToCitation() {
  const hash = window.location.hash.substring(1);
  const page = Number(new URLSearchParams(hash).get('page'));
  if (Number.isInteger(page) && page >= 1 && page <= pdfViewer.pagesCount) {
    pdfViewer.currentPageNumber = page;
    pdfViewer.getPageView(page - 1).div.scrollIntoView({ block: 'start' });
  } else {
    pdfLinkService.setHash(hash);
  }
}
eventBus.on('pagesloaded', navigateToCitation);
window.addEventListener('hashchange', navigateToCitation);

// Define how the "search" query parameter is handled.
eventBus.on('findfromurlhash', function(evt) {
  eventBus.dispatch('find', {
    source: evt.source,
    type: '',
    query: evt.query,
    caseSensitive: false,
    entireWord: false,
    highlightAll: false,
    findPrevious: false,
    matchDiacritics: true,
  });
});

// Load and initialize the document.
const pdfDocument = await pdfjsLib.getDocument({
  url: fileUrl,
  enableXfa: true,
}).promise;

pdfViewer.setDocument(pdfDocument);
pdfLinkService.setDocument(pdfDocument, null);
