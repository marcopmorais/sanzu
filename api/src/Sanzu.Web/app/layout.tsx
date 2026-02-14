import "./globals.css";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Sanzu Web",
  description: "Sanzu frontend implementation baseline"
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
