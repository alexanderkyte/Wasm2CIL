;; Test `if` operator

(module
  ;; Auxiliary definition
  (func $dummy)

  (func (export "empty") (param i32)
    (if (get_local 0) (then))
    (if (get_local 0) (then) (else))
    (if $l (get_local 0) (then))
    (if $l (get_local 0) (then) (else))
  )

  (func (export "singular") (param i32) (result i32)
    (if (get_local 0) (then (nop)))
    (if (get_local 0) (then (nop)) (else (nop)))
    (if (result i32) (get_local 0) (then (i32.const 7)) (else (i32.const 8)))
  )

  (func (export "multi") (param i32) (result i32)
    (if (get_local 0) (then (call $dummy) (call $dummy) (call $dummy)))
    (if (get_local 0) (then) (else (call $dummy) (call $dummy) (call $dummy)))
    (if (result i32) (get_local 0)
      (then (call $dummy) (call $dummy) (i32.const 8))
      (else (call $dummy) (call $dummy) (i32.const 9))
    )
  )

  (func (export "nested") (param i32 i32) (result i32)
    (if (result i32) (get_local 0)
      (then
        (if (get_local 1) (then (call $dummy) (block) (nop)))
        (if (get_local 1) (then) (else (call $dummy) (block) (nop)))
        (if (result i32) (get_local 1)
          (then (call $dummy) (i32.const 9))
          (else (call $dummy) (i32.const 10))
        )
      )
      (else
        (if (get_local 1) (then (call $dummy) (block) (nop)))
        (if (get_local 1) (then) (else (call $dummy) (block) (nop)))
        (if (result i32) (get_local 1)
          (then (call $dummy) (i32.const 10))
          (else (call $dummy) (i32.const 11))
        )
      )
    )
  )

  (func (export "as-unary-operand") (param i32) (result i32)
    (i32.ctz
      (if (result i32) (get_local 0)
        (then (call $dummy) (i32.const 13))
        (else (call $dummy) (i32.const -13))
      )
    )
  )
  (func (export "as-binary-operand") (param i32 i32) (result i32)
    (i32.mul
      (if (result i32) (get_local 0)
        (then (call $dummy) (i32.const 3))
        (else (call $dummy) (i32.const -3))
      )
      (if (result i32) (get_local 1)
        (then (call $dummy) (i32.const 4))
        (else (call $dummy) (i32.const -5))
      )
    )
  )
  (func (export "as-test-operand") (param i32) (result i32)
    (i32.eqz
      (if (result i32) (get_local 0)
        (then (call $dummy) (i32.const 13))
        (else (call $dummy) (i32.const 0))
      )
    )
  )
  (func (export "as-compare-operand") (param i32 i32) (result i32)
    (f32.gt
      (if (result f32) (get_local 0)
        (then (call $dummy) (f32.const 3))
        (else (call $dummy) (f32.const -3))
      )
      (if (result f32) (get_local 1)
        (then (call $dummy) (f32.const 4))
        (else (call $dummy) (f32.const -4))
      )
    )
  )

  (func (export "break-bare") (result i32)
    (if (i32.const 1) (then (br 0) (unreachable)))
    (if (i32.const 1) (then (br 0) (unreachable)) (else (unreachable)))
    (if (i32.const 0) (then (unreachable)) (else (br 0) (unreachable)))
    (if (i32.const 1) (then (br_if 0 (i32.const 1)) (unreachable)))
    (if (i32.const 1) (then (br_if 0 (i32.const 1)) (unreachable)) (else (unreachable)))
    (if (i32.const 0) (then (unreachable)) (else (br_if 0 (i32.const 1)) (unreachable)))
    (if (i32.const 1) (then (br_table 0 (i32.const 0)) (unreachable)))
    (if (i32.const 1) (then (br_table 0 (i32.const 0)) (unreachable)) (else (unreachable)))
    (if (i32.const 0) (then (unreachable)) (else (br_table 0 (i32.const 0)) (unreachable)))
    (i32.const 19)
  )

  (func (export "break-value") (param i32) (result i32)
    (if (result i32) (get_local 0)
      (then (br 0 (i32.const 18)) (i32.const 19))
      (else (br 0 (i32.const 21)) (i32.const 20))
    )
  )

  (func (export "effects") (param i32) (result i32)
    (local i32)
    (if
      (block (result i32) (set_local 1 (i32.const 1)) (get_local 0))
      (then
        (set_local 1 (i32.mul (get_local 1) (i32.const 3)))
        (set_local 1 (i32.sub (get_local 1) (i32.const 5)))
        (set_local 1 (i32.mul (get_local 1) (i32.const 7)))
        (br 0)
        (set_local 1 (i32.mul (get_local 1) (i32.const 100)))
      )
      (else
        (set_local 1 (i32.mul (get_local 1) (i32.const 5)))
        (set_local 1 (i32.sub (get_local 1) (i32.const 7)))
        (set_local 1 (i32.mul (get_local 1) (i32.const 3)))
        (br 0)
        (set_local 1 (i32.mul (get_local 1) (i32.const 1000)))
      )
    )
    (get_local 1)
  )
)

