
window.deliveryQr = {
    print: async function (shopName, shopCode, imageUrl) {

        // نجلب صورة QR من نفس الموقع وبجلسة المستخدم
        const response = await fetch(imageUrl, {
            credentials: "same-origin"
        });

        if (!response.ok) {
            alert("تعذر تحميل رمز QR للطباعة");
            return;
        }

        const blob = await response.blob();

        const qrDataUrl = await new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result);
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });

        const printWindow = window.open("", "_blank");

        if (!printWindow) {
            alert("المتصفح منع نافذة الطباعة. اسمح بالنوافذ المنبثقة.");
            return;
        }

        const escapeHtml = value => String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#39;");

        printWindow.document.open();

        printWindow.document.write(`
            <!DOCTYPE html>
            <html lang="ar" dir="rtl">
            <head>
                <meta charset="UTF-8">
                <title>QR - ${escapeHtml(shopCode)}</title>

                <style>
                    @page {
                        size: A5 portrait;
                        margin: 12mm;
                    }

                    * {
                        box-sizing: border-box;
                    }

                    body {
                        margin: 0;
                        font-family: Arial, Tahoma, sans-serif;
                        color: #17243b;
                        text-align: center;
                    }

                    .print-card {
                        border: 2px solid #e2e8f0;
                        border-radius: 18px;
                        padding: 28px 20px;
                    }

                    .brand {
                        font-size: 14px;
                        color: #2563eb;
                        font-weight: bold;
                        margin-bottom: 24px;
                    }

                    h1 {
                        font-size: 25px;
                        margin: 0 0 12px;
                    }

                    .code {
                        font-size: 15px;
                        color: #64748b;
                        margin-bottom: 24px;
                        direction: ltr;
                    }

                    .qr {
                        width: 240px;
                        height: 240px;
                        object-fit: contain;
                        max-width: 100%;
                    }

                    .instruction {
                        margin-top: 22px;
                        font-size: 15px;
                        font-weight: bold;
                    }

                    .footer {
                        margin-top: 15px;
                        font-size: 12px;
                        color: #94a3b8;
                    }
                </style>
            </head>

            <body>
                <div class="print-card">
                    <div class="brand">DELIVERY TRACKER</div>

                    <h1>${escapeHtml(shopName)}</h1>

                    <div class="code">${escapeHtml(shopCode)}</div>

                    <img class="qr" src="${qrDataUrl}" alt="QR Code">

                    <div class="instruction">
                        امسح الرمز لتسجيل عملية التسليم
                    </div>

                    <div class="footer">
                        Delivery Tracker System
                    </div>
                </div>
            </body>
            </html>
        `);

        printWindow.document.close();

        const img = printWindow.document.querySelector(".qr");

        await new Promise(resolve => {
            if (img.complete) {
                resolve();
                return;
            }

            img.onload = resolve;
            img.onerror = resolve;
        });

        printWindow.focus();
        printWindow.print();
    }
};
