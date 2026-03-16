-- =============================================================
-- V007 - Seed sample supplier data
-- RiskScreening Platform - Demo / Initial Data
-- =============================================================
-- Inserts 35 real-world entities sourced from:
--   (WB)   World Bank Listing of Ineligible Firms
--          https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms
--   (OFAC) OFAC Specially Designated Nationals (SDN) List
--          https://sanctionssearch.ofac.treas.gov/
--   (ICIJ) ICIJ Offshore Leaks Database
--          https://offshoreleaks.icij.org
--
-- Country codes follow ISO 3166-1 alpha-2 (validated by CountryCode value object).
-- TAX IDs are synthetic 11-digit codes used for demo purposes only.
-- Contact fields are NULL when not publicly available (common for sanctioned entities).
-- Script is idempotent: skips insert if any supplier row already exists.
-- =============================================================

IF EXISTS (SELECT 1 FROM suppliers)
    RETURN;

-- =============================================================
-- WORLD BANK - Debarred / Ineligible Firms
-- =============================================================

INSERT INTO suppliers (id, legal_name, commercial_name, tax_id, country,
    contact_phone, contact_email, website, address, annual_billing_usd,
    risk_level, status, is_deleted, notes, created_at, updated_at)
VALUES
(
    NEWID(),
    'Africa Enablers GmbH',
    'Africa Enablers',
    '99100000001',
    'CH',
    NULL, NULL, NULL,
    'Switzerland',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-03-06 to 2025-06-06 | Fraudulent Practice — misrepresented past experience in Somali SCORE Program.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Solutions for Development Support (Pvt.) Ltd.',
    'SDS Pakistan',
    '99100000002',
    'PK',
    NULL, NULL, NULL,
    'Pakistan',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-03-27 to 2027-10-20 | Fraudulent Practice — failed to disclose affiliation in Sindh Resilience Project tenders.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Taihan Electric Wire Co., Ltd.',
    'Taihan Cable & Solution',
    '99100000003',
    'KR',
    '+82-2-3480-7114',
    'info@taihan.com',
    'https://www.taihan.com',
    '132 Toegye-ro, Jung-gu, Seoul, Republic of Korea',
    850000000.00,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-04-03 to 2026-10-03 | Collusive and Obstructive Practice — bid collusion and document concealment in Mongolia E-Health Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Ernst & Young LLP, Kenya',
    'EY Kenya',
    '99100000004',
    'KE',
    '+254-20-2886000',
    'ey.kenya@ke.ey.com',
    'https://www.ey.com/en_ke',
    'Kenya Re Towers, Upper Hill, Nairobi, Kenya',
    12000000.00,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-06-26 to 2026-12-26 | Fraudulent and Corrupt Practices — undisclosed conflicts of interest and improper payments in SCORE / PFM II Somalia.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'PT. LPPSLH Konsultan',
    'LPPSLH Konsultan',
    '99100000005',
    'ID',
    NULL, NULL, NULL,
    'Indonesia',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-07-10 to 2029-12-10 | Fraudulent, Corrupt, and Obstructive Practices — overbilling and improper hiring in Indonesia project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Marseille for Engineering & Trading S.A.L. Offshore',
    'Marseille Engineering',
    '99100000006',
    'JO',
    NULL, NULL, NULL,
    'Jordan',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-05-14 to 2026-11-14 | Corrupt Practice — offered payments to influence bid evaluation in Iraq Emergency Operation.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Future Netwings Solutions Private Limited',
    'FNSPL India',
    '99100000007',
    'IN',
    NULL, NULL, NULL,
    'India',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-06-24 to 2025-09-24 | Collusive Practice — coordinated bid preparation in West Bengal Gram Panchayats Program II.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'BETS Consulting Services Limited',
    'BETS Consulting',
    '99100000008',
    'BD',
    NULL, NULL, NULL,
    'Dhaka, Bangladesh',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2023-11-01 to 2025-09-01 | Corrupt and Fraudulent Practices — payments to officials and falsified invoices in Chittagong Water Supply Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Consultores en Ingeniería S.A. de C.V.',
    'CINSA Honduras',
    '99100000009',
    'HN',
    NULL, NULL, NULL,
    'Honduras',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2023-10-04 to 2025-04-04 | Fraudulent Practice — misrepresented conflict of interest in Climate Resilience Program Honduras.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Burhani Engineers Ltd.',
    'Burhani Engineers',
    '99100000010',
    'KE',
    NULL, NULL, NULL,
    'Nairobi, Kenya',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2023-03-08 to 2025-03-08 | Fraudulent Practice — misrepresented prior experience in Uganda Energy for Rural Transformation III Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Viva Atlantic Limited',
    'Viva Atlantic',
    '99100000011',
    'NG',
    NULL, NULL, NULL,
    'Lagos, Nigeria',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2025-01-16 to 2027-07-16 | Fraudulent, Collusive, and Corrupt Practices — falsified authorization letters and bribery of officials.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Technology House Limited',
    'Technology House NG',
    '99100000012',
    'NG',
    NULL, NULL, NULL,
    'Lagos, Nigeria',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2025-01-16 to 2027-07-16 | Fraudulent and Collusive Practices — received confidential tender information and misrepresented conflicts of interest.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Kontrolmatik Teknoloji Enerji Ve Mühendislik A.S.',
    'Kontrolmatik',
    '99100000013',
    'TR',
    '+90-312-219-6262',
    'info@kontrolmatik.com.tr',
    'https://www.kontrolmatik.com.tr',
    'Mustafa Kemal Mah. 2120. Sok. No:13, 06530 Çankaya, Ankara, Türkiye',
    45000000.00,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2025-12-11 to 2027-12-11 | Fraudulent and Obstructive Practices — fabricated past performance documents in Iraq Emergency Operation Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Consultoria Técnica Cia. Ltda.',
    'CONTEC Ecuador',
    '99100000014',
    'EC',
    NULL, NULL, NULL,
    'Guayaquil, Ecuador',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2026-02-18 to 2027-05-18 | Fraudulent Practice — undisclosed conflict of interest in Guayaquil Wastewater Management Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'GenKey Solutions B.V.',
    'GenKey Solutions',
    '99100000015',
    'NL',
    '+31-20-205-0700',
    'info@genkey.com',
    'https://www.genkey.com',
    'Koningin Julianaplein 10, 2595 AA Den Haag, Netherlands',
    8000000.00,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2025-07-08 to 2027-01-08 | Fraudulent Practice — failed to disclose agent commissions in Liberia Social Safety Nets Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'TPF GETINSA EUROESTUDIOS S.L.',
    'TPF Euroestudios',
    '99100000016',
    'ES',
    '+34-91-343-1000',
    'tpf@tpf.eu',
    'https://www.tpf.eu',
    'Ramón de Aguinaga 8, Madrid 28028, Spain',
    120000000.00,
    'HIGH', 'PENDING', 0,
    'WB: Sanctioned 2024-02-29 to 2027-11-28 | Corrupt and Collusive Practices.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'IQVIA Consulting and Information Services India',
    'IQVIA India',
    '99100000017',
    'IN',
    '+91-22-6190-5800',
    'india.info@iqvia.com',
    'https://www.iqvia.com',
    'One Lodha Place, 17th Floor, Senapati Bapat Marg, Mumbai 400 013, India',
    500000000.00,
    'HIGH', 'PENDING', 0,
    'WB: Sanctioned 2025-06-17 to 2026-12-16 | Fraudulent Practices.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'L.S.D. Construction & Supplies',
    'LSD Construction',
    '99100000018',
    'PH',
    NULL, NULL, NULL,
    'Philippines',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2025-05-28 approx. 54 months | Collusive, Fraudulent, and Corrupt Practices — secret subcontracting and improper payments in Philippine Rural Development Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Panaque, S.R.L.',
    'Panaque SRL',
    '99100000019',
    'IT',
    NULL, NULL, NULL,
    'Italy',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2025-03-19 to 2027-03-19 | Collusive, Fraudulent, and Corrupt Practices — improper payment to public official in Montenegro Agriculture Project.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Community Resilience Initiative',
    'Community Resilience PK',
    '99100000020',
    'PK',
    NULL, NULL, NULL,
    'Pakistan',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-03-27 to 2027-10-20 | Fraudulent Practice — failed to disclose affiliation in Sindh Resilience Project tenders.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Venus Softwares',
    'Venus Softwares India',
    '99100000021',
    'IN',
    NULL, NULL, NULL,
    'West Bengal, India',
    NULL,
    'HIGH', 'PENDING', 0,
    'WB: Debarred 2024-09-26 to 2025-12-26 | Collusive Practice — coordinated bid preparation in West Bengal Gram Panchayats Program II.',
    GETUTCDATE(), GETUTCDATE()
),

-- =============================================================
-- OFAC SDN LIST - Sanctioned Entities
-- =============================================================

(
    NEWID(),
    'Bank Markazi Jomhouri Islami Iran',
    'Central Bank of Iran',
    '99200000001',
    'IR',
    '+98-21-29954',
    NULL,
    'https://www.cbi.ir',
    'Mirdamad Blvd., Tehran, Islamic Republic of Iran',
    NULL,
    'HIGH', 'PENDING', 0,
    'OFAC SDN: Programs — IRAN; SDGT; IRGC; IFSR | Linked to IRGC-Qods Force and Hizballah. Subject to secondary sanctions.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Bank Maskan',
    'Bank Maskan Iran',
    '99200000002',
    'IR',
    '+98-21-85450',
    NULL,
    'https://www.bank-maskan.ir',
    'Vanak Square, Tehran, Islamic Republic of Iran',
    NULL,
    'HIGH', 'PENDING', 0,
    'OFAC SDN: Programs — IRAN; IRAN-EO13902 | All offices worldwide. Subject to secondary sanctions.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Bank Refah Kargaran',
    'Bank Refah',
    '99200000003',
    'IR',
    '+98-21-88990',
    NULL,
    'https://www.bankrefah.ir',
    'Taleghani Ave., Tehran, Islamic Republic of Iran',
    NULL,
    'HIGH', 'PENDING', 0,
    'OFAC SDN: Programs — IRAN; IRAN-EO13902 | All offices worldwide. Subject to secondary sanctions.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Bank Keshavarzi Iran',
    'Agricultural Bank of Iran',
    '99200000004',
    'IR',
    '+98-21-61010',
    NULL,
    'https://www.agri-bank.com',
    'Patrice Lumumba St., Tehran, Islamic Republic of Iran',
    NULL,
    'HIGH', 'PENDING', 0,
    'OFAC SDN: Programs — IRAN; IRAN-EO13902 | All offices worldwide. Subject to secondary sanctions.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Bank Saderat PLC',
    'Bank Saderat',
    '99200000005',
    'GB',
    '+44-20-7283-8000',
    NULL,
    NULL,
    'Ibex House, The Minories, London EC3N 1DY, United Kingdom',
    NULL,
    'HIGH', 'PENDING', 0,
    'OFAC SDN: Programs — IRAN; SDGT; IFSR | UK Company No. 01126618. All offices worldwide. Subject to secondary sanctions.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Havin Bank Limited',
    'Havin Bank',
    '99200000006',
    'GB',
    '+44-20-7606-6151',
    NULL,
    NULL,
    'Cannon Bridge House, 25 Dowgate Hill, London EC4R 2YA, United Kingdom',
    NULL,
    'HIGH', 'PENDING', 0,
    'OFAC SDN: Program — CUBA | SWIFT/BIC: HAVIGB2L. UK Company No. 01074897.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Banco Nacional de Cuba',
    'BNC Cuba',
    '99200000007',
    'CU',
    '+53-7-859-8025',
    NULL,
    'https://www.bnc.cu',
    'Cuba St. and Lamparilla St., Havana, Cuba',
    NULL,
    'HIGH', 'PENDING', 0,
    'OFAC SDN: Program — CUBA | State-owned national bank of Cuba.',
    GETUTCDATE(), GETUTCDATE()
),

-- =============================================================
-- ICIJ OFFSHORE LEAKS DATABASE - Corporate Entities
-- =============================================================

(
    NEWID(),
    'Oceania International Consultants (BVI) Company Limited',
    'Oceania International BVI',
    '99300000001',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — Hong Kong | Data From — Panama Papers (Mossack Fonseca). Intermediary: Fung & Chan.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'The African Diamond Company Limited',
    'African Diamond Co.',
    '99300000002',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — Switzerland | Data From — Panama Papers. Intermediary: JTC Suisse SA (Geneva).',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Deca International Holding Company Limited',
    'Deca International Holding',
    '99300000003',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — China | Data From — Panama Papers. Officers: Deng Xiao-Ou, Jiang Kai (shareholders).',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Aciram Real Estate Ventures Ltd.',
    'Aciram Real Estate',
    '99300000004',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — USA (Miami, FL) | Data From — Panama Papers. Associated: Intercorp International Group.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Compaq Cayman Holdings Company',
    'Compaq Cayman Holdings',
    '99300000005',
    'KY',
    NULL, NULL, NULL,
    'Palm Grove House, Road Town, Tortola, Cayman Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — Cayman Islands | Linked To — USA | Data From — Paradise Papers (Appleby). Service provider: Codan Trust Company.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Papyrus Trading Company Limited',
    'Papyrus Trading',
    '99300000006',
    'MT',
    NULL, NULL, NULL,
    'Tarxien Road, Gudja, Malta',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — Malta | Linked To — Malta | Data From — Paradise Papers (Malta Corporate Registry). Auditor: Deloitte Audit Ltd.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Dorado Maritime Company Limited',
    'Dorado Maritime',
    '99300000007',
    'MT',
    NULL, NULL, NULL,
    'Exchange Buildings, Republic Street, Valletta VLT 1117, Malta',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — Malta | Linked To — Malta, Libya, Liechtenstein, USA | Data From — Paradise Papers (Malta Corporate Registry). Status: Struck Off.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Pan African Shipping Company Limited',
    'Pan African Shipping',
    '99300000008',
    'BS',
    NULL, NULL, NULL,
    'Nassau, Bahamas',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — Bahamas | Data From — Bahamas Leaks. Intermediary: The Alexander Corporate Group.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Corporacion San Bernardino S.A.',
    'Corp. San Bernardino',
    '99300000009',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — El Salvador | Data From — Pandora Papers (OMC). Beneficial owner: José Ricardo Poma Delgado.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Bancard International Investment Inc.',
    'Bancard International',
    '99300000010',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — Chile | Data From — Pandora Papers (OMC). Power of attorney held 2004-2017.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'The Belgian Food Company Holdings Ltd',
    'Belgian Food Holdings',
    '99300000011',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — United Kingdom (Sark, Channel Islands) | Data From — Offshore Leaks. Intermediary: Atlas Maritime Services.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Well-Done Group Company Limited',
    'Well-Done Group',
    '99300000012',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — China | Data From — Panama Papers. Intermediary: Beijing Voson Int. IP Attorney Co., Ltd.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Jensen Company Limited',
    'Jensen Co. BVI',
    '99300000013',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — Jersey, United Kingdom | Data From — Panama Papers. Intermediary: Barclays Trust Company Jersey Limited.',
    GETUTCDATE(), GETUTCDATE()
),
(
    NEWID(),
    'Amila Company Limited',
    'Amila Co. BVI',
    '99300000014',
    'VG',
    NULL, NULL, NULL,
    'Road Town, Tortola, British Virgin Islands',
    NULL,
    'MEDIUM', 'PENDING', 0,
    'ICIJ Offshore Leaks: Jurisdiction — British Virgin Islands | Linked To — Liechtenstein | Data From — Panama Papers. Intermediary: Audina Treuhand AG (Vaduz).',
    GETUTCDATE(), GETUTCDATE()
);
