# Sample JSON Storage Layout

This document illustrates how various JSON structures are represented in database tables after ingestion into Lattice.

---

## Table Overview

| Table | Purpose |
|-------|---------|
| `documents` | Document metadata (id, collection, schema reference) |
| `schemas` | Unique schema definitions identified by hash |
| `schemaelements` | Individual keys within each schema with data types |
| `indextablemappings` | Maps dot-notation keys to dynamic index table names |
| `index_{hash}` | Dynamic tables storing actual values per key |

---

## 1. Simple JSON Object

### Input JSON
```json
{
  "Name": "Alice",
  "Age": 30,
  "Active": true
}
```

### Table Representations

#### `documents`
| id | collectionid | schemaid | name |
|----|--------------|----------|------|
| `doc_001` | `coll_abc` | `schema_xyz` | `alice.json` |

#### `schemas`
| id | name | hash |
|----|------|------|
| `schema_xyz` | `simple_person` | `a1b2c3d4...` |

#### `schemaelements`
| id | schemaid | position | key | datatype | nullable |
|----|----------|----------|-----|----------|----------|
| `elem_1` | `schema_xyz` | 0 | `Name` | `string` | false |
| `elem_2` | `schema_xyz` | 1 | `Age` | `integer` | false |
| `elem_3` | `schema_xyz` | 2 | `Active` | `boolean` | false |

#### `indextablemappings`
| id | key | tablename |
|----|-----|-----------|
| `map_1` | `Name` | `index_a1b2c3` |
| `map_2` | `Age` | `index_d4e5f6` |
| `map_3` | `Active` | `index_g7h8i9` |

#### Dynamic Index Tables

**`index_a1b2c3`** (for key `Name`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_1` | `doc_001` | NULL | `Alice` |

**`index_d4e5f6`** (for key `Age`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_2` | `doc_001` | NULL | `30` |

**`index_g7h8i9`** (for key `Active`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_3` | `doc_001` | NULL | `true` |

---

## 2. JSON Array (Top-Level)

### Input JSON
```json
{
  "Colors": ["Red", "Green", "Blue"]
}
```

### Table Representations

#### `documents`
| id | collectionid | schemaid | name |
|----|--------------|----------|------|
| `doc_002` | `coll_abc` | `schema_arr` | `colors.json` |

#### `schemas`
| id | name | hash |
|----|------|------|
| `schema_arr` | `color_list` | `b2c3d4e5...` |

#### `schemaelements`
| id | schemaid | position | key | datatype | nullable |
|----|----------|----------|-----|----------|----------|
| `elem_4` | `schema_arr` | 0 | `Colors` | `array<string>` | false |

#### `indextablemappings`
| id | key | tablename |
|----|-----|-----------|
| `map_4` | `Colors` | `index_j1k2l3` |

#### Dynamic Index Tables

**`index_j1k2l3`** (for key `Colors`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_4` | `doc_002` | 0 | `Red` |
| `val_5` | `doc_002` | 1 | `Green` |
| `val_6` | `doc_002` | 2 | `Blue` |

**Note:** Each array element gets its own row with a `position` value indicating its index in the array. Non-array values have `position = NULL`.

---

## 3. Deeply Nested JSON Object

### Input JSON
```json
{
  "Company": {
    "Name": "Acme Corp",
    "Location": {
      "Country": "USA",
      "Address": {
        "City": "Austin",
        "State": "TX",
        "Zip": "78701"
      }
    }
  }
}
```

### Table Representations

#### `documents`
| id | collectionid | schemaid | name |
|----|--------------|----------|------|
| `doc_003` | `coll_abc` | `schema_deep` | `company.json` |

#### `schemas`
| id | name | hash |
|----|------|------|
| `schema_deep` | `company_info` | `c3d4e5f6...` |

#### `schemaelements`

Nested paths are represented using **dot-notation**:

| id | schemaid | position | key | datatype | nullable |
|----|----------|----------|-----|----------|----------|
| `elem_5` | `schema_deep` | 0 | `Company.Name` | `string` | false |
| `elem_6` | `schema_deep` | 1 | `Company.Location.Country` | `string` | false |
| `elem_7` | `schema_deep` | 2 | `Company.Location.Address.City` | `string` | false |
| `elem_8` | `schema_deep` | 3 | `Company.Location.Address.State` | `string` | false |
| `elem_9` | `schema_deep` | 4 | `Company.Location.Address.Zip` | `string` | false |

**Note:** Only leaf values (primitives) are stored. Intermediate objects (`Company`, `Location`, `Address`) are not stored as separate entries—their structure is preserved in the dot-notation keys.

#### `indextablemappings`
| id | key | tablename |
|----|-----|-----------|
| `map_5` | `Company.Name` | `index_m4n5o6` |
| `map_6` | `Company.Location.Country` | `index_p7q8r9` |
| `map_7` | `Company.Location.Address.City` | `index_s1t2u3` |
| `map_8` | `Company.Location.Address.State` | `index_v4w5x6` |
| `map_9` | `Company.Location.Address.Zip` | `index_y7z8a9` |

#### Dynamic Index Tables

**`index_m4n5o6`** (for key `Company.Name`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_7` | `doc_003` | NULL | `Acme Corp` |

**`index_p7q8r9`** (for key `Company.Location.Country`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_8` | `doc_003` | NULL | `USA` |

**`index_s1t2u3`** (for key `Company.Location.Address.City`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_9` | `doc_003` | NULL | `Austin` |

**`index_v4w5x6`** (for key `Company.Location.Address.State`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_10` | `doc_003` | NULL | `TX` |

**`index_y7z8a9`** (for key `Company.Location.Address.Zip`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_11` | `doc_003` | NULL | `78701` |

---

## 4. Complex JSON with Mixed Literals, Objects, and Arrays

### Input JSON
```json
{
  "Person": {
    "First": "Joel",
    "Last": "Christner",
    "Emails": ["joel@work.com", "joel@home.com"],
    "Addresses": [
      {
        "Type": "Home",
        "City": "Austin",
        "Primary": true
      },
      {
        "Type": "Work",
        "City": "San Jose",
        "Primary": false
      }
    ]
  },
  "Age": 47,
  "Active": true,
  "Score": 98.6,
  "Notes": null
}
```

### Table Representations

#### `documents`
| id | collectionid | schemaid | name |
|----|--------------|----------|------|
| `doc_004` | `coll_abc` | `schema_complex` | `person_full.json` |

#### `schemas`
| id | name | hash |
|----|------|------|
| `schema_complex` | `person_detailed` | `d4e5f6g7...` |

#### `schemaelements`

| id | schemaid | position | key | datatype | nullable |
|----|----------|----------|-----|----------|----------|
| `elem_10` | `schema_complex` | 0 | `Person.First` | `string` | false |
| `elem_11` | `schema_complex` | 1 | `Person.Last` | `string` | false |
| `elem_12` | `schema_complex` | 2 | `Person.Emails` | `array<string>` | false |
| `elem_13` | `schema_complex` | 3 | `Person.Addresses` | `array<object>` | false |
| `elem_14` | `schema_complex` | 4 | `Person.Addresses.Type` | `string` | false |
| `elem_15` | `schema_complex` | 5 | `Person.Addresses.City` | `string` | false |
| `elem_16` | `schema_complex` | 6 | `Person.Addresses.Primary` | `boolean` | false |
| `elem_17` | `schema_complex` | 7 | `Age` | `integer` | false |
| `elem_18` | `schema_complex` | 8 | `Active` | `boolean` | false |
| `elem_19` | `schema_complex` | 9 | `Score` | `number` | false |
| `elem_20` | `schema_complex` | 10 | `Notes` | `null` | true |

**Notes:**
- `array<string>` indicates an array of strings (`Emails`)
- `array<object>` indicates an array of objects (`Addresses`)
- Nested object fields within arrays use dot-notation (`Person.Addresses.Type`)
- `null` is stored as a distinct data type
- `number` represents floating-point values (vs `integer` for whole numbers)

#### `indextablemappings`
| id | key | tablename |
|----|-----|-----------|
| `map_10` | `Person.First` | `index_b1c2d3` |
| `map_11` | `Person.Last` | `index_e4f5g6` |
| `map_12` | `Person.Emails` | `index_h7i8j9` |
| `map_13` | `Person.Addresses.Type` | `index_k1l2m3` |
| `map_14` | `Person.Addresses.City` | `index_n4o5p6` |
| `map_15` | `Person.Addresses.Primary` | `index_q7r8s9` |
| `map_16` | `Age` | `index_t1u2v3` |
| `map_17` | `Active` | `index_w4x5y6` |
| `map_18` | `Score` | `index_z7a8b9` |
| `map_19` | `Notes` | `index_c1d2e3` |

**Note:** `Person.Addresses` (the array container itself) is tracked in `schemaelements` for schema purposes but does not get its own index table—only the leaf values within the array objects are indexed.

#### Dynamic Index Tables

**`index_b1c2d3`** (for key `Person.First`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_12` | `doc_004` | NULL | `Joel` |

**`index_e4f5g6`** (for key `Person.Last`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_13` | `doc_004` | NULL | `Christner` |

**`index_h7i8j9`** (for key `Person.Emails`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_14` | `doc_004` | 0 | `joel@work.com` |
| `val_15` | `doc_004` | 1 | `joel@home.com` |

**`index_k1l2m3`** (for key `Person.Addresses.Type`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_16` | `doc_004` | 0 | `Home` |
| `val_17` | `doc_004` | 1 | `Work` |

**`index_n4o5p6`** (for key `Person.Addresses.City`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_18` | `doc_004` | 0 | `Austin` |
| `val_19` | `doc_004` | 1 | `San Jose` |

**`index_q7r8s9`** (for key `Person.Addresses.Primary`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_20` | `doc_004` | 0 | `true` |
| `val_21` | `doc_004` | 1 | `false` |

**`index_t1u2v3`** (for key `Age`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_22` | `doc_004` | NULL | `47` |

**`index_w4x5y6`** (for key `Active`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_23` | `doc_004` | NULL | `true` |

**`index_z7a8b9`** (for key `Score`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_24` | `doc_004` | NULL | `98.6` |

**`index_c1d2e3`** (for key `Notes`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_25` | `doc_004` | NULL | NULL |

---

## 5. Root-Level Array

When the input JSON is an array rather than an object, the elements are stored with a synthetic `$` key representing the root.

### Input JSON
```json
["Apple", "Banana", "Cherry"]
```

### Table Representations

#### `documents`
| id | collectionid | schemaid | name |
|----|--------------|----------|------|
| `doc_005` | `coll_abc` | `schema_rootarr` | `fruits.json` |

#### `schemas`
| id | name | hash |
|----|------|------|
| `schema_rootarr` | `string_array` | `e5f6g7h8...` |

#### `schemaelements`
| id | schemaid | position | key | datatype | nullable |
|----|----------|----------|-----|----------|----------|
| `elem_21` | `schema_rootarr` | 0 | `$` | `array<string>` | false |

**Note:** The `$` key represents the document root when the top-level value is not an object.

#### `indextablemappings`
| id | key | tablename |
|----|-----|-----------|
| `map_20` | `$` | `index_f3g4h5` |

#### Dynamic Index Tables

**`index_f3g4h5`** (for key `$`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_26` | `doc_005` | 0 | `Apple` |
| `val_27` | `doc_005` | 1 | `Banana` |
| `val_28` | `doc_005` | 2 | `Cherry` |

---

## 6. Root-Level Literal

When the input JSON is a single literal value (string, number, boolean, or null), it is stored using the synthetic `$` key.

### Input JSON
```json
"Hello, World!"
```

### Table Representations

#### `documents`
| id | collectionid | schemaid | name |
|----|--------------|----------|------|
| `doc_006` | `coll_abc` | `schema_literal` | `greeting.json` |

#### `schemas`
| id | name | hash |
|----|------|------|
| `schema_literal` | `string_literal` | `f6g7h8i9...` |

#### `schemaelements`
| id | schemaid | position | key | datatype | nullable |
|----|----------|----------|-----|----------|----------|
| `elem_22` | `schema_literal` | 0 | `$` | `string` | false |

#### `indextablemappings`
| id | key | tablename |
|----|-----|-----------|
| `map_21` | `$` | `index_i6j7k8` |

#### Dynamic Index Tables

**`index_i6j7k8`** (for key `$`)
| id | documentid | position | value |
|----|------------|----------|-------|
| `val_29` | `doc_006` | NULL | `Hello, World!` |

**Note:** Numeric and boolean literals work the same way:

- Input: `42` → stored as `value = 42` with `datatype = 'integer'`
- Input: `3.14159` → stored as `value = 3.14159` with `datatype = 'number'`
- Input: `true` → stored as `value = true` with `datatype = 'boolean'`
- Input: `null` → stored as `value = NULL` with `datatype = 'null'`

---

## Key Takeaways

1. **Dot-Notation Flattening**: Nested object paths become dot-separated keys (e.g., `Person.Addresses.City`)

2. **One Index Table Per Key**: Each unique key (across all documents) gets its own dedicated index table, enabling efficient queries per field

3. **Array Handling**:
   - Each array element is stored as a separate row
   - The `position` column tracks the element's index (0-based)
   - Non-array values have `position = NULL`

4. **Only Leaf Values Are Indexed**: Intermediate objects/containers are not stored as values—only primitive leaf values (strings, numbers, booleans, null) appear in index tables

5. **Schema Deduplication**: Documents with identical key structures share the same schema (matched by SHA256 hash)

6. **Type Preservation**: Data types are tracked in `schemaelements` (`string`, `integer`, `number`, `boolean`, `null`, `array<T>`)

7. **Null Handling**: Null values are explicitly stored with `value = NULL` and tracked as `datatype = 'null'` in the schema

8. **Original JSON Preserved**: The raw JSON file is saved to disk at `{documentsDirectory}/{documentId}.json` for full document retrieval
