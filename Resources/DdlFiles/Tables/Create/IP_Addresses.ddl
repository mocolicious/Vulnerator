CREATE TABLE IF NOT EXISTS IP_Addresses (
    IP_Address_ID INTEGER PRIMARY KEY,
    IP_Address NVARCHAR (25) NOT NULL UNIQUE ON CONFLICT IGNORE
);